using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NitroxModel.BelowZero.Enums;
using NitroxModel.Logger;

namespace NitroxEvents.BelowZero.Core
{
    /// <summary>
    /// Below Zero事件管理器 - 直接套用Below Zero的事件系统架构
    /// </summary>
    public static class BelowZeroEventManager
    {
        private static readonly ConcurrentDictionary<Type, List<IEventHandler>> EventHandlers = new();
        private static readonly ConcurrentDictionary<Type, List<IEventProcessor>> EventProcessors = new();
        private static readonly object EventLock = new();
        private static bool isInitialized = false;

        /// <summary>
        /// 事件统计信息
        /// </summary>
        public static EventStatistics Statistics { get; private set; } = new();

        /// <summary>
        /// 初始化事件管理器
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
            {
                Log.Warn("Below Zero事件管理器已经初始化");
                return;
            }

            Log.Info("初始化Below Zero事件管理器...");
            
            // 自动注册所有事件处理器和处理者
            RegisterAllHandlers();
            RegisterAllProcessors();
            
            isInitialized = true;
            Log.Info($"Below Zero事件管理器初始化完成，注册了 {EventHandlers.Count} 个处理器类型和 {EventProcessors.Count} 个处理者类型");
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        public static void RegisterHandler<T>(IEventHandler<T> handler) where T : IBelowZeroEvent
        {
            var eventType = typeof(T);
            EventHandlers.AddOrUpdate(eventType, 
                new List<IEventHandler> { handler },
                (key, existing) =>
                {
                    existing.Add(handler);
                    return existing;
                });
            
            Log.Debug($"注册Below Zero事件处理器: {handler.GetType().Name} -> {eventType.Name}");
        }

        /// <summary>
        /// 注册事件处理者
        /// </summary>
        public static void RegisterProcessor<T>(IEventProcessor<T> processor) where T : IBelowZeroEvent
        {
            var eventType = typeof(T);
            EventProcessors.AddOrUpdate(eventType,
                new List<IEventProcessor> { processor },
                (key, existing) =>
                {
                    existing.Add(processor);
                    return existing;
                });
            
            Log.Debug($"注册Below Zero事件处理者: {processor.GetType().Name} -> {eventType.Name}");
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public static void TriggerEvent<T>(T eventData) where T : IBelowZeroEvent
        {
            if (!isInitialized)
            {
                Log.Error("Below Zero事件管理器未初始化");
                return;
            }

            var eventType = typeof(T);
            Statistics.RecordEvent(eventType);

            lock (EventLock)
            {
                try
                {
                    // 先执行处理器（Handler）
                    if (EventHandlers.TryGetValue(eventType, out var handlers))
                    {
                        foreach (var handler in handlers.Cast<IEventHandler<T>>())
                        {
                            try
                            {
                                handler.Handle(eventData);
                                Statistics.RecordSuccess(eventType);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Below Zero事件处理器异常: {handler.GetType().Name}, 事件: {eventType.Name}, 错误: {ex.Message}");
                                Statistics.RecordError(eventType);
                            }
                        }
                    }

                    // 再执行处理者（Processor）
                    if (EventProcessors.TryGetValue(eventType, out var processors))
                    {
                        foreach (var processor in processors.Cast<IEventProcessor<T>>())
                        {
                            try
                            {
                                processor.Process(eventData);
                                Statistics.RecordSuccess(eventType);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Below Zero事件处理者异常: {processor.GetType().Name}, 事件: {eventType.Name}, 错误: {ex.Message}");
                                Statistics.RecordError(eventType);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Below Zero事件触发异常: {eventType.Name}, 错误: {ex.Message}");
                    Statistics.RecordError(eventType);
                }
            }
        }

        /// <summary>
        /// 清理事件管理器
        /// </summary>
        public static void Cleanup()
        {
            Log.Info("清理Below Zero事件管理器...");
            EventHandlers.Clear();
            EventProcessors.Clear();
            Statistics.Reset();
            isInitialized = false;
        }

        /// <summary>
        /// 获取诊断信息
        /// </summary>
        public static string GetDiagnostics()
        {
            var diagnostics = "Below Zero事件管理器诊断信息:\n";
            diagnostics += $"已注册处理器类型: {EventHandlers.Count}\n";
            diagnostics += $"已注册处理者类型: {EventProcessors.Count}\n";
            diagnostics += $"总事件数量: {Statistics.TotalEvents}\n";
            diagnostics += $"成功处理: {Statistics.SuccessfulEvents}\n";
            diagnostics += $"处理失败: {Statistics.FailedEvents}\n";
            
            return diagnostics;
        }

        /// <summary>
        /// 自动注册所有事件处理器
        /// </summary>
        private static void RegisterAllHandlers()
        {
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.Contains("NitroxEvents.BelowZero") || a.FullName.Contains("BelowZero"))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    var handler = Activator.CreateInstance(handlerType) as IEventHandler;
                    if (handler != null)
                    {
                        var eventType = handlerType.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                            .GetGenericArguments()[0];

                        EventHandlers.AddOrUpdate(eventType,
                            new List<IEventHandler> { handler },
                            (key, existing) =>
                            {
                                existing.Add(handler);
                                return existing;
                            });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"无法注册事件处理器: {handlerType.Name}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 自动注册所有事件处理者
        /// </summary>
        private static void RegisterAllProcessors()
        {
            var processorTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.Contains("NitroxEvents.BelowZero") || a.FullName.Contains("BelowZero"))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventProcessor<>)))
                .ToList();

            foreach (var processorType in processorTypes)
            {
                try
                {
                    var processor = Activator.CreateInstance(processorType) as IEventProcessor;
                    if (processor != null)
                    {
                        var eventType = processorType.GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventProcessor<>))
                            .GetGenericArguments()[0];

                        EventProcessors.AddOrUpdate(eventType,
                            new List<IEventProcessor> { processor },
                            (key, existing) =>
                            {
                                existing.Add(processor);
                                return existing;
                            });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"无法注册事件处理者: {processorType.Name}, 错误: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Below Zero事件接口
    /// </summary>
    public interface IBelowZeroEvent
    {
        /// <summary>
        /// 事件ID
        /// </summary>
        string EventId { get; }
        
        /// <summary>
        /// 事件时间戳
        /// </summary>
        DateTime Timestamp { get; }
        
        /// <summary>
        /// 处理类型
        /// </summary>
        ProcessType ProcessType { get; }
    }

    /// <summary>
    /// 事件处理器接口
    /// </summary>
    public interface IEventHandler
    {
    }

    /// <summary>
    /// 泛型事件处理器接口
    /// </summary>
    public interface IEventHandler<in T> : IEventHandler where T : IBelowZeroEvent
    {
        void Handle(T eventData);
    }

    /// <summary>
    /// 事件处理者接口
    /// </summary>
    public interface IEventProcessor
    {
    }

    /// <summary>
    /// 泛型事件处理者接口
    /// </summary>
    public interface IEventProcessor<in T> : IEventProcessor where T : IBelowZeroEvent
    {
        void Process(T eventData);
    }

    /// <summary>
    /// 事件统计信息
    /// </summary>
    public class EventStatistics
    {
        private readonly ConcurrentDictionary<Type, int> eventCounts = new();
        private readonly ConcurrentDictionary<Type, int> successCounts = new();
        private readonly ConcurrentDictionary<Type, int> errorCounts = new();

        public int TotalEvents { get; private set; }
        public int SuccessfulEvents { get; private set; }
        public int FailedEvents { get; private set; }

        public void RecordEvent(Type eventType)
        {
            eventCounts.AddOrUpdate(eventType, 1, (key, oldValue) => oldValue + 1);
            TotalEvents++;
        }

        public void RecordSuccess(Type eventType)
        {
            successCounts.AddOrUpdate(eventType, 1, (key, oldValue) => oldValue + 1);
            SuccessfulEvents++;
        }

        public void RecordError(Type eventType)
        {
            errorCounts.AddOrUpdate(eventType, 1, (key, oldValue) => oldValue + 1);
            FailedEvents++;
        }

        public void Reset()
        {
            eventCounts.Clear();
            successCounts.Clear();
            errorCounts.Clear();
            TotalEvents = 0;
            SuccessfulEvents = 0;
            FailedEvents = 0;
        }

        public Dictionary<Type, int> GetEventCounts() => eventCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        public Dictionary<Type, int> GetSuccessCounts() => successCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        public Dictionary<Type, int> GetErrorCounts() => errorCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
