using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityEditor.Build.OneBuild
{

    [Serializable]
    public class EditorTask : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string guid = Guid.NewGuid().ToString();
        [SerializeField]
        private ActionQueue onStarted = new ActionQueue();
        [SerializeField]
        internal ActionQueue tasks = new ActionQueue();
        [SerializeField]
        private ActionQueue onTaskBefore = new ActionQueue();
        [SerializeField]
        private ActionQueue onTaskAfter = new ActionQueue();
        [SerializeField]
        private ActionQueue onEnded = new ActionQueue();

        [SerializeField]
        private Status status;
        enum Status
        {
            Initialize,
            Started,
            Task,
            Ended,
        }

        private bool isRunning;

        private static string TaskQueueKey = typeof(EditorTask).FullName + ".queue";

        private static List<EditorTask> taskQueues;
        private static bool isUpdateTask;
        public bool progressBarEnabled = true;




        internal static List<EditorTask> TaskQueues
        {
            get
            {
                if (taskQueues == null)
                {
                    string str = PlayerPrefs.GetString(TaskQueueKey, null);
                    if (!string.IsNullOrEmpty(str))
                    {
                        try
                        {
                            var queue = (TaskQueue)JsonUtility.FromJson(str, typeof(TaskQueue));
                            if (queue != null && queue.HasDeserializeError)
                            {
                                queue = null;
                                PlayerPrefs.SetString(TaskQueueKey, null);
                                Debug.LogError("Cancel EditorTask");
                            }
                            if (queue != null)
                            {
                                if (queue.tasks != null)
                                {
                                    taskQueues = queue.tasks;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            Cancel();
                        }
                    }
                    if (taskQueues == null)
                        taskQueues = new List<EditorTask>();
                }
                return taskQueues;
            }
        }

        static bool CanExecute
        {
            get
            {
                if (EditorApplication.isCompiling)
                    return false;
                if (EditorApplication.isUpdating)
                    return false;
                if (EditorApplication.isPlaying)
                    return false;
                return true;
            }
        }

        static void Cancel()
        {
            if (taskQueues == null || taskQueues.Count == 0)
                return;
            Debug.LogError("Cancel EditorTask");
            taskQueues.Clear();
            Save();
        }


        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            if (TaskQueues.Count > 0)
            {
                Debug.Log(EditorOneBuild.BuildLogPrefix+ "task continue on refresh break");
                if (Application.isBatchMode)
                {
                    UpdateTask();
                }
                else
                {
                    EditorApplication.update -= UpdateTask;
                    EditorApplication.update += UpdateTask;
                }
            }
        }

        private static void UpdateTask()
        {
            if (isUpdateTask)
                return;

            isUpdateTask = true;
            try
            {
                for (int i = 0; CanExecute && i < TaskQueues.Count; i++)
                {
                    var queue = TaskQueues[i];
                    while (CanExecute)
                    {
                        TaskAction action = null;
                        switch (queue.status)
                        {
                            case Status.Initialize:
                                queue.onStarted.current = 0;
                                queue.onEnded.current = 0;
                                queue.status = Status.Started;
                                Save();
                                break;
                            case Status.Started:
                                while (queue.onStarted.HasNext())
                                {
                                    queue.onStarted.Next().Callback();
                                }
                                queue.status = Status.Task;
                                Save();
                                break;
                            case Status.Task:
                                action = Next(queue.tasks);
                                if (action == null)
                                {
                                    queue.status = Status.Ended;
                                    Save();
                                }
                                else
                                {
                                    queue.onTaskBefore.current = 0;
                                    queue.onTaskAfter.current = 0;
                                }
                                break;
                            default: break;
                        }

                        if (queue.status == Status.Ended)
                        {
                            TaskQueues.RemoveAt(i);
                            Save();
                            while (queue.onEnded.HasNext())
                            {
                                queue.onEnded.Next().Callback();
                            }
                            i--;
                            break;
                        }
                        if (action == null)
                            continue;
                        if (action.Callback != null)
                        {
                            bool showProgressBar = false;
                            try
                            {
                                if (queue.progressBarEnabled)
                                {
                                    EditorUtility.DisplayProgressBar(action.Display, null, 0);
                                    showProgressBar = true;
                                }

                                if (queue.status == Status.Task)
                                {
                                    while (queue.onTaskBefore.HasNext())
                                        queue.onTaskBefore.Next().Callback();
                                }
                                DateTime dt = DateTime.Now;
                                action.Callback();
                                Debug.Log(EditorOneBuild.BuildLogPrefix + "step " + queue.tasks.current + "/" + queue.tasks.Count + ", [" + action.Callback.Method.DeclaringType.FullName + "." + action.Callback.Method.Name + "] time: " + (DateTime.Now - dt).TotalSeconds.ToString("0.#") + "s");
                                
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("build error " + action.Callback.Method.Name);
                                Debug.LogException(ex);
                                queue.status = Status.Ended;
                                Save();
                            }
                            finally
                            {

                                if (showProgressBar)
                                {
                                    showProgressBar = false;
                                    EditorUtility.ClearProgressBar();
                                }

                                if (queue.status == Status.Task)
                                {
                                    while (queue.onTaskAfter.HasNext())
                                        queue.onTaskAfter.Next().Callback();
                                }

                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                isUpdateTask = false;
            }

            EditorApplication.update -= UpdateTask;
        }

        static TaskAction Next(ActionQueue actions)
        {
            TaskAction action = null;
            if (actions.HasNext())
            {
                action = actions.tasks[actions.current];
                actions.current++;
                Save();
            }
            return action;
        }

        static void ExecuteAction(ActionQueue actions)
        {

        }


        public EditorTask Run()
        {
            if (isRunning)
                throw new Exception("Task already run");
            isRunning = true;
            TaskQueues.Add(this);
            Save();

            if (TaskQueues.Count > 0)
            {
                if (Application.isBatchMode)
                {
                    UpdateTask();
                }
                else
                {
                    EditorApplication.update -= UpdateTask;
                    EditorApplication.update += UpdateTask;
                }
            }
            return this;
        }

        public static void Reset()
        {
            taskQueues = null;
            PlayerPrefs.DeleteKey(TaskQueueKey);
        }



        [Serializable]
        class TaskQueue : ISerializationCallbackReceiver
        {
            [SerializeField]
            public List<EditorTask> tasks = new List<EditorTask>();

            public bool HasDeserializeError { get; private set; }
            public void OnAfterDeserialize()
            {
                HasDeserializeError = false;
                if (tasks != null)
                {
                    foreach (var item in tasks)
                    {
                        if (item.HasDeserializeError)
                        {
                            HasDeserializeError = true;
                            break;
                        }
                    }
                }
            }

            public void OnBeforeSerialize()
            {

            }
        }

        [Serializable]
        internal class ActionQueue : ISerializationCallbackReceiver, IEnumerable<TaskAction>
        {
            [SerializeField]
            public List<TaskAction> tasks = new List<TaskAction>();
            [SerializeField]
            public int current;

            public bool HasDeserializeError { get; private set; }

            public int Count { get => tasks.Count; }

            public bool HasNext()
            {
                return current < tasks.Count;
            }

            public TaskAction Next()
            {
                return tasks[current++];
            }

            public void Add(TaskAction action)
            {
                if (action == null)
                    return;
                tasks.Add(action);
            }

            public IEnumerator<TaskAction> GetEnumerator()
            {
                for (; current < tasks.Count; current++)
                {
                    yield return tasks[current];
                }
            }

            public void OnAfterDeserialize()
            {
                HasDeserializeError = false;
                if (tasks != null)
                {
                    foreach (var item in tasks)
                    {
                        if (item.HasDeserializeError)
                        {
                            HasDeserializeError = true;
                            break;
                        }
                    }
                }
            }

            public void OnBeforeSerialize()
            {

            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }


        private static void Save()
        {
            string str = JsonUtility.ToJson(new TaskQueue() { tasks = TaskQueues });
            //Debug.Log("save:" + str + "," + TaskQueues.Count);
            PlayerPrefs.SetString(TaskQueueKey, str);
            PlayerPrefs.Save();
        }


        [Serializable]
        internal class TaskAction : ISerializationCallbackReceiver
        {
            [SerializeField]
            private SerializationDelegate callback;
            public string Display;

            public Action Callback
            {
                get { return (Action)callback.Delegate; }
                set { callback = new SerializationDelegate(value); }
            }
            public bool HasDeserializeError { get; private set; }

            public void OnAfterDeserialize()
            {
                HasDeserializeError = false;
                if (callback != null)
                {
                    HasDeserializeError = callback.HasDeserializeError;
                }
            }

            public void OnBeforeSerialize()
            {
            }
        }

        public EditorTask AddAction(Action callback, string display = null)
        {
            tasks.Add(new TaskAction() { Callback = callback, Display = display });
            return this;
        }

        public EditorTask AddOnStarted(Action callback, string display = null)
        {
            onStarted.Add(new TaskAction() { Callback = callback, Display = display });
            return this;
        }

        public EditorTask AddOnEnded(Action callback, string display = null)
        {
            onEnded.Add(new TaskAction() { Callback = callback, Display = display });
            return this;
        }

        public EditorTask AddOnTaskBefore(Action callback, string display = null)
        {
            onTaskBefore.Add(new TaskAction() { Callback = callback, Display = display });
            return this;
        }

        public EditorTask AddOnTaskAfter(Action callback, string display = null)
        {
            onTaskAfter.Add(new TaskAction() { Callback = callback, Display = display });
            return this;
        }


        public bool HasDeserializeError { get; private set; }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            HasDeserializeError = false;
            if (tasks != null)
            {
                HasDeserializeError = tasks.HasDeserializeError;
            }
        }

        [Serializable]
        class SerializationDelegate : ISerializationCallbackReceiver
        {
            [SerializeField]
            private string delStr;
            private Delegate del;


            public SerializationDelegate() { }

            public SerializationDelegate(Delegate del)
            {
                this.del = del;
            }

            public Delegate Delegate
            {
                get { return del; }
                set { del = value; }
            }
            public bool HasDeserializeError { get; private set; }

            public void OnAfterDeserialize()
            {
                del = null;
                if (!string.IsNullOrEmpty(delStr))
                {
                    foreach (var itemPart in delStr.Split('|'))
                    {
                        try
                        {
                            string[] parts = itemPart.Split(new char[] { ';' }, 2);
                            string methodName = parts[0];
                            string typeName = parts[1];
                            Type type = Type.GetType(typeName, true);
                            var method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                               .Where(o => o.ToString() == methodName).FirstOrDefault();
                            if (method == null)
                                throw new Exception("not found method:" + methodName);

                            //throw new Exception("method not is static " + method);
                            Action _del;
                            if (method.IsStatic)
                            {
                                _del = (Action)Delegate.CreateDelegate(typeof(Action), method);
                            }
                            else
                            {
                                var obj = Activator.CreateInstance(type);
                                _del = (Action)Delegate.CreateDelegate(typeof(Action), obj, method);
                            }
                            if (del == null)
                                del = _del;
                            else
                                del = Delegate.Combine(del, _del);
                        }
                        catch (Exception ex)
                        {
                            HasDeserializeError = true;
                            throw ex;
                        }
                    }
                    delStr = null;
                }
            }

            public void OnBeforeSerialize()
            {
                delStr = null;
                HasDeserializeError = false;
                if (del != null)
                {
                    StringBuilder sb = new StringBuilder();
                    bool first = true;
                    foreach (var invoke in del.GetInvocationList())
                    {
                        if (!first)
                            sb.Append("|");
                        else
                            first = false;
                        sb.Append(invoke.Method.ToString())
                            .Append(";")
                            .Append(invoke.Method.DeclaringType.AssemblyQualifiedName);
                    }
                    delStr = sb.ToString();
                }

            }
        }

    }



}