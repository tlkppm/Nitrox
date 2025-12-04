using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NitroxModel.Logger;
using NitroxModel.Platforms.OS.Shared;

namespace Nitrox.Launcher.Models.Utils;

/// <summary>
/// Below Zero专用的游戏注入入口点 - 套用原Nitrox架构
/// </summary>
public static class BelowZeroEntryPatch
{
    public const string GAME_ASSEMBLY_NAME = "Assembly-CSharp.dll";
    public const string NITROX_ASSEMBLY_NAME = "NitroxPatcher-BelowZero.dll";
    public const string GAME_ASSEMBLY_MODIFIED_NAME = "Assembly-CSharp-BelowZero.dll";

    private const string NITROX_ENTRY_TYPE_NAME = "BelowZeroMain";
    private const string NITROX_ENTRY_METHOD_NAME = "Execute";

    private const string TARGET_TYPE_NAME = "PlatformUtils";
    private const string TARGET_METHOD_NAME = "Awake";

    private const string NITROX_EXECUTE_INSTRUCTION = "System.Void NitroxPatcher.BelowZeroMain::Execute()";

    /// <summary>
    /// 注入Below Zero入口点到游戏Assembly-CSharp.dll
    /// </summary>
    public static async Task Apply(string belowZeroBasePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(belowZeroBasePath, nameof(belowZeroBasePath));

        string belowZeroManagedPath = Path.Combine(belowZeroBasePath, GameInfo.SubnauticaBelowZero.DataFolder, "Managed");
        string assemblyCSharp = Path.Combine(belowZeroManagedPath, GAME_ASSEMBLY_NAME);
        string nitroxPatcherPath = Path.Combine(belowZeroManagedPath, NITROX_ASSEMBLY_NAME);
        string modifiedAssemblyCSharp = Path.Combine(belowZeroManagedPath, GAME_ASSEMBLY_MODIFIED_NAME);

        Log.Debug("检查Below Zero代码是否存在");

        if (File.Exists(modifiedAssemblyCSharp))
        {
            // 避免AssemblyCSharp.dll被删除但AssemblyCSharp-BelowZero.dll存在的情况
            if (!File.Exists(assemblyCSharp))
            {
                Log.Error($"无效状态，{GAME_ASSEMBLY_NAME} 未找到，但 {GAME_ASSEMBLY_MODIFIED_NAME} 存在。请验证Below Zero安装。");
                FileSystem.Instance.ReplaceFile(modifiedAssemblyCSharp, assemblyCSharp);
            }
            else
            {
                Log.Debug($"{GAME_ASSEMBLY_MODIFIED_NAME} 已存在，正在删除");
                Exception copyError = RetryWait(() => File.Delete(modifiedAssemblyCSharp), 100, 5);
                if (copyError != null)
                {
                    throw copyError;
                }
            }
        }

        byte[] cachedSha256ForFile = await Hashing.GetCachedSha256ByFilePath(assemblyCSharp);
        byte[] currentCodeFileSha256 = await Hashing.GetSha256(assemblyCSharp);
        if (cachedSha256ForFile.SequenceEqual(currentCodeFileSha256))
        {
            Log.Info("Below Zero已经有Nitrox入口补丁");
            return;
        }

        Log.Debug($"添加Below Zero Nitrox入口点，因为代码文件哈希不匹配 [{Convert.ToHexStringLower(cachedSha256ForFile)}] != [{Convert.ToHexStringLower(currentCodeFileSha256)}]");

        /*
         * private void Awake()
         * {
         *     NitroxPatcher.BelowZeroMain.Execute(); <--- [插入的代码行]
         *     if (PlatformUtils._main != null)
         *     {
         *         Debug.LogError("Multiple PlatformUtils instances found in scene!", this);
         *         Debug.Break();
         *         global::UnityEngine.Object.DestroyImmediate(base.gameObject);
         *         return;
         *     }
         *     PlatformUtils._main = this;
         *     global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
         *     base.StartCoroutine(this.PlatformInitAsync());
         * }
        */
        
        // 检查NitroxPatcher-BelowZero.dll是否存在
        if (!File.Exists(nitroxPatcherPath))
        {
            Log.Warn($"Below Zero Patcher ({NITROX_ASSEMBLY_NAME}) 未找到，尝试复制原版Patcher");
            string originalPatcherPath = Path.Combine(belowZeroManagedPath, "NitroxPatcher.dll");
            if (File.Exists(originalPatcherPath))
            {
                File.Copy(originalPatcherPath, nitroxPatcherPath, true);
                Log.Debug("已复制原版Patcher作为Below Zero Patcher");
            }
            else
            {
                throw new FileNotFoundException($"找不到Below Zero Patcher: {nitroxPatcherPath}");
            }
        }

        using (ModuleDefMD module = ModuleDefMD.Load(assemblyCSharp))
        using (ModuleDefMD nitroxPatcherAssembly = ModuleDefMD.Load(nitroxPatcherPath))
        {
            // 查找Nitrox主类 (使用标准Main类作为回退)
            TypeDef nitroxMainDefinition = nitroxPatcherAssembly.GetTypes().FirstOrDefault(x => x.Name == NITROX_ENTRY_TYPE_NAME) 
                                         ?? nitroxPatcherAssembly.GetTypes().FirstOrDefault(x => x.Name == "Main");
            
            if (nitroxMainDefinition == null)
            {
                throw new InvalidOperationException($"在 {NITROX_ASSEMBLY_NAME} 中找不到入口点类");
            }

            MethodDef executeMethodDefinition = nitroxMainDefinition.Methods.FirstOrDefault(x => x.Name == NITROX_ENTRY_METHOD_NAME);
            if (executeMethodDefinition == null)
            {
                throw new InvalidOperationException($"在入口点类中找不到 {NITROX_ENTRY_METHOD_NAME} 方法");
            }

            MemberRef executeMethodReference = module.Import(executeMethodDefinition);

            TypeDef gameInputType = module.GetTypes().First(x => x.FullName == TARGET_TYPE_NAME);
            MethodDef awakeMethod = gameInputType.Methods.First(x => x.Name == TARGET_METHOD_NAME);

            Instruction callNitroxExecuteInstruction = OpCodes.Call.ToInstruction(executeMethodReference);

            if (awakeMethod.Body.Instructions[0].Operand is MemberRef refA && callNitroxExecuteInstruction.Operand is MemberRef refB && refA.FullName == refB.FullName)
            {
                Log.Warn("Below Zero Nitrox入口点已经被补丁");
                return;
            }

            awakeMethod.Body.Instructions.Insert(0, callNitroxExecuteInstruction);
            module.Write(modifiedAssemblyCSharp);

            Log.Debug($"写入程序集到 {GAME_ASSEMBLY_MODIFIED_NAME}");
            File.SetAttributes(assemblyCSharp, System.IO.FileAttributes.Normal);
        }

        // 程序集可能被其他代码或程序使用，为安全起见重试
        Log.Debug($"删除 {GAME_ASSEMBLY_NAME}");
        Exception? error = RetryWait(() => File.Delete(assemblyCSharp), 100, 5);
        if (error != null)
        {
            throw error;
        }

        FileSystem.Instance.ReplaceFile(modifiedAssemblyCSharp, assemblyCSharp);
        Log.Debug("已添加Below Zero Nitrox入口点");

        Log.Debug("在缓存中存储Nitrox变异代码文件的SHA256");
        Log.Debug($"代码文件SHA256: {Convert.ToHexStringLower(await Hashing.GetAndStoreSha256ForFile(assemblyCSharp))}");
    }

    /// <summary>
    /// 从Below Zero的Assembly-CSharp.dll中移除Nitrox入口点
    /// </summary>
    public static void Remove(string belowZeroBasePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(belowZeroBasePath, nameof(belowZeroBasePath));

        Log.Debug("从Below Zero移除Nitrox入口点");

        string belowZeroManagedPath = Path.Combine(belowZeroBasePath, GameInfo.SubnauticaBelowZero.DataFolder, "Managed");
        string assemblyCSharp = Path.Combine(belowZeroManagedPath, GAME_ASSEMBLY_NAME);
        string modifiedAssemblyCSharp = Path.Combine(belowZeroManagedPath, GAME_ASSEMBLY_MODIFIED_NAME);

        using (ModuleDefMD module = ModuleDefMD.Load(assemblyCSharp))
        {
            TypeDef gameInputType = module.GetTypes().First(x => x.FullName == TARGET_TYPE_NAME);
            MethodDef awakeMethod = gameInputType.Methods.First(x => x.Name == TARGET_METHOD_NAME);

            IList<Instruction> methodInstructions = awakeMethod.Body.Instructions;
            int nitroxExecuteInstructionIndex = FindNitroxExecuteInstructionIndex(methodInstructions);
            if (nitroxExecuteInstructionIndex == -1)
            {
                Log.Debug($"在 {TARGET_TYPE_NAME}:{TARGET_METHOD_NAME} 中未找到Below Zero Nitrox入口点");
                return;
            }
            do
            {
                methodInstructions.RemoveAt(nitroxExecuteInstructionIndex);
            } while ((nitroxExecuteInstructionIndex = FindNitroxExecuteInstructionIndex(methodInstructions)) >= 0);
            module.Write(modifiedAssemblyCSharp);

            File.SetAttributes(assemblyCSharp, System.IO.FileAttributes.Normal);
        }

        FileSystem.Instance.ReplaceFile(modifiedAssemblyCSharp, assemblyCSharp);
        Log.Debug("已从Below Zero移除Nitrox入口点");
    }

    private static int FindNitroxExecuteInstructionIndex(IList<Instruction> methodInstructions)
    {
        for (int instructionIndex = 0; instructionIndex < methodInstructions.Count; instructionIndex++)
        {
            string instruction = methodInstructions[instructionIndex].Operand?.ToString();

            if (instruction == NITROX_EXECUTE_INSTRUCTION || instruction?.Contains("NitroxPatcher") == true)
            {
                return instructionIndex;
            }
        }

        return -1;
    }

    private static Exception? RetryWait(Action action, int interval, int retries = 0)
    {
        Exception lastException = null;
        while (retries >= 0)
        {
            try
            {
                retries--;
                action();
                return null;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Task.Delay(interval).Wait();
            }
        }
        return lastException;
    }

    public static bool IsPatchApplied(string belowZeroBasePath)
    {
        try
        {
            string belowZeroManagedPath = Path.Combine(belowZeroBasePath, GameInfo.SubnauticaBelowZero.DataFolder, "Managed");
            string gameInputPath = Path.Combine(belowZeroManagedPath, GAME_ASSEMBLY_NAME);

            if (!File.Exists(gameInputPath))
            {
                return false;
            }

            using (ModuleDefMD module = ModuleDefMD.Load(gameInputPath))
            {
                TypeDef gameInputType = module.GetTypes().First(x => x.FullName == TARGET_TYPE_NAME);
                MethodDef awakeMethod = gameInputType.Methods.First(x => x.Name == TARGET_METHOD_NAME);

                string firstInstruction = awakeMethod.Body.Instructions[0]?.ToString();
                return firstInstruction == NITROX_EXECUTE_INSTRUCTION || firstInstruction?.Contains("NitroxPatcher") == true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"检查Below Zero补丁状态时出错: {ex.Message}");
            return false;
        }
    }
}
