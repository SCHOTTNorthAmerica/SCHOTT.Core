using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCHOTT.Core.Utilities
{
    /// <summary>
    /// A MessageBroker used for passing data between SCHOTT classes.
    /// </summary>
    public class MessageBroker
    {
        /// <summary>
        /// The context in which to send a message.
        /// </summary>
        public enum MessageContext
        {
            /// <summary>
            /// This context will pass the data directly to the user on the same thread as the GUI.
            /// It is safe to update GUI controls directly from this call.
            /// This *WILL* block the calling thread.
            /// Care should be taken here. If the user blocks the calling thread, it can cause unintended consequences.
            /// </summary>
            DirectToGui,

            /// <summary>
            /// This context will create a new thread synced with the GUI. 
            /// This is the safest way to process messages, but has the most overhead.
            /// It is safe to update GUI controls directly from this call.
            /// This will not block the calling thread.
            /// </summary>
            NewThreadToGui,

            /// <summary>
            /// This context will pass the data directly to the user on the same thread that generated the data.
            /// This has the least amount of overhead, and should be used when just putting data into local memory.
            /// It is *NOT* safe to update GUI controls directly from this call.
            /// This *WILL* block the calling thread.
            /// Care should be taken here. If the user blocks the calling thread, it can cause unintended consequences.
            /// </summary>
            DirectToData
        }

        private class Message
        {
            public MessageContext Context { get; }
            public Action<object> Action { get; }

            public Message(MessageContext context, Action<object> action)
            {
                Action = action;
                Context = context;
            }
        }

        private readonly SynchronizationContext _syncContext = AsyncOperationManager.SynchronizationContext;
        private readonly Dictionary<object, List<Message>> _actionDictionary = new Dictionary<object, List<Message>>();

        /// <summary>
        /// This allows a class to register to be notified of a message event.
        /// </summary>
        /// <typeparam name="TU">The key type to use for the message index.</typeparam>
        /// <typeparam name="T">The action type to be executed by the message.</typeparam>
        /// <param name="key">The key to look up all registerd actions.</param>
        /// <param name="context">The context to use when calling the action.</param>
        /// <param name="action">The lambda expression to execute on the message call.</param>
        public void Register<TU, T>(TU key, MessageContext context, Action<T> action)
        {
            if (!_actionDictionary.ContainsKey(key))
                _actionDictionary.Add(key, new List<Message>());

            _actionDictionary[key].Add(new Message(context, o => action((T)o)));
        }

        /// <summary>
        /// Used by a class to fire the event.
        /// </summary>
        /// <typeparam name="TU">The key type to use for the message index.</typeparam>
        /// <typeparam name="T">The action type to be executed by the message.</typeparam>
        /// <param name="key">The key to look up all registerd actions.</param>
        /// <param name="args">The args to pass the registered actions.</param>
        public void RunActions<TU, T>(TU key, T args)
        {
            if (!_actionDictionary.ContainsKey(key))
                return;

            // execute direct data calls          
            foreach (var functionCall in _actionDictionary[key].Where(x => x.Context == MessageContext.DirectToData).Select(y => y.Action))
            {
                functionCall(args);
            }

            // execute direct GUI calls            
            foreach (var functionCall in _actionDictionary[key].Where(x => x.Context == MessageContext.DirectToGui).Select(y => y.Action))
            {
                _syncContext.Post(a => functionCall(a), args);
            }

            // execute threaded GUI calls
            foreach (var functionCall in _actionDictionary[key].Where(x => x.Context == MessageContext.NewThreadToGui).Select(y => y.Action))
            {
                Task.Factory.StartNew(() => _syncContext.Post(a => functionCall(a), args));
            }
        }
    }

}
