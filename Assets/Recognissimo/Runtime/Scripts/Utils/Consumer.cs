using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Recognissimo.Utils
{
    public class Consumer<T> 
    {
        private BlockingCollection<T> _input;
        private Thread _thread;

        public Action onStart;
        public Action<T> onConsume;
        public Action onStop;
        public Action onFail;

        public bool IsActive => _isActive;
        private volatile bool _isActive;

        public void Start()
        {
            _thread?.Join();
            
            _input = new BlockingCollection<T>();
            
            _thread = new Thread(Routine);
            _thread.Start();

            _isActive = true;
        }

        public void Feed(T inputData)
        {
            _input?.Add(inputData);
        }
        
        public void Stop()
        {
            _input.CompleteAdding();
        }
        
        private void Routine()
        {
            try
            {
                onStart?.Invoke();

                if (onConsume != null)
                {
                    foreach (var item in _input.GetConsumingEnumerable())
                    {
                        onConsume.Invoke(item);
                    }
                }

                onStop?.Invoke();
            }
            catch
            {
                onFail?.Invoke();
            }
            finally
            {
                _isActive = false;
            }
        }
    }
}