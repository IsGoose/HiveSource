using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hive.Application
{
    public class HiveProcess
    {
        private List<Task<object>> _tasks;
        private readonly object _lock;
        
        public static bool IsSetup = false;
        public List<Task<object>> Tasks => _tasks;
        
        public HiveProcess()
        {
            _tasks = new List<Task<object>>();
            _lock = new object();
        }
        
        public object InvokeTask(MethodInfo action,object[] parameters)
        {
            lock (_lock)
            {
                var task = Task.Factory.StartNew(() => action.Invoke(null, parameters));
                return task.Result;
            }
            
        }
        public int InvokeTaskAsync(MethodInfo action, object[] parameters)
        {
            lock (_lock)
            {
                var task = Task.Factory.StartNew(() => action.Invoke(null, parameters));
                _tasks.Add(task);
                return task.Id;
            }
        }
        public void InvokeTaskAndForget(MethodInfo action, object[] parameters) => Task.Factory.StartNew(() => action.Invoke(null, parameters));
        public void InvokeTaskAndForget(Task t) => t.Start();

        public object GetTaskResult(int taskId, out bool ready)
        {
            lock (_lock)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    ready = false;
                    return "NullTask";
                }

                if (task.Status == TaskStatus.Faulted)
                {
                    ready = false;
                    return "Faulted";
                }

                if (task.Status == TaskStatus.RanToCompletion)
                {
                    ready = true;
                    return task.Result;
                }

                ready = false;
                return "NotReady";
            }
        }
        public void RemoveTask(int taskId)
        {
            lock (_lock)
            {
                _tasks.RemoveAt(_tasks.FindIndex(t => t.Id == taskId));
            }
        }

        public void RemoveTask(Task<object> task)
        {
            lock (_lock)
            {
                _tasks.RemoveAt(_tasks.FindIndex(t => t.Id == task.Id));
            }
        }
    }
}