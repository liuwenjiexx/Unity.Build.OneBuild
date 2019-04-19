using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


[Serializable]
public class EditorTask
{
    [SerializeField]
    private string guid = Guid.NewGuid().ToString();
    [SerializeField]
    private List<TaskAction> tasks = new List<TaskAction>();


    private static string TaskQueueKey = typeof(EditorTask).FullName + ".queue";

    private static List<EditorTask> taskQueues;
    private static bool isUpdateTask;



    internal static List<EditorTask> TaskQueues
    {
        get
        {
            if (taskQueues == null)
            {
                string str = EditorPrefs.GetString(TaskQueueKey, null);
                if (!string.IsNullOrEmpty(str))
                {
                    try
                    {
                        var queue = (TaskQueue)JsonUtility.FromJson(str, typeof(TaskQueue));
                        if (queue.tasks != null)
                            taskQueues = queue.tasks;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
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

    [InitializeOnLoadMethod]
    static void InitializeOnLoadMethod()
    {
        if (TaskQueues.Count > 0)
        {
            EditorApplication.delayCall += () =>
            {
                UpdateTask();
            };
        }
    }

    private static void UpdateTask()
    {
        if (isUpdateTask)
            return;
 
        isUpdateTask = true;
        bool changed = false;
        for (int i = 0; CanExecute && i < TaskQueues.Count; i++)
        {
            var queue = TaskQueues[i];
            while (CanExecute && queue.tasks.Count > 0)
            {
                var task = queue.tasks[0];
                queue.tasks.RemoveAt(0);
                Save();
                //  changed = true;
                if (task.Callback != null)
                {
                    try
                    {
                        task.Callback();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        queue.tasks.Clear();
                        Save();
                    }
                }
            }
            if (queue.tasks.Count == 0)
            {
                TaskQueues.RemoveAt(i);
                changed = true;
                Save();
                i--;
            }
        }

        if (changed)
        {
            Save();
        }
         
        isUpdateTask = false;
    }


    public EditorTask Run()
    {
        if (TaskQueues.Where(o => o.guid == guid).Count() > 0)
            throw new Exception("Task already run");
        TaskQueues.Add(this);
        Save();

        UpdateTask();
        return this;
    }
    [Serializable]
    class TaskQueue
    {
        [SerializeField]
        public List<EditorTask> tasks = new List<EditorTask>();
    }
    private static void Save()
    {
        string str = JsonUtility.ToJson(new TaskQueue() { tasks = TaskQueues });
        //Debug.Log("save:" + str + "," + TaskQueues.Count);
        EditorPrefs.SetString(TaskQueueKey, str);

    }


    [Serializable]
    class TaskAction
    {
        [SerializeField]
        private SerializationDelegate callback;

        public Action Callback
        {
            get { return (Action)callback.Delegate; }
            set { callback = new SerializationDelegate(value); }
        }

    }

    public EditorTask Add(Action task)
    {
        tasks.Add(new TaskAction() { Callback = task });
        return this;
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

        public void OnAfterDeserialize()
        {
            del = null;
            if (!string.IsNullOrEmpty(delStr))
            {
                string[] parts = delStr.Split(new char[] { ';' }, 2);
                string typeName = parts[0];
                string methodName = parts[1];
                Type type = Type.GetType(typeName, true);
                var method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                   .Where(o => o.ToString() == methodName).FirstOrDefault();
                if (method == null)
                    throw new Exception("not found method:" + methodName);
                if (!method.IsStatic)
                    throw new Exception("method not is static " + method);
                del = (Action)Delegate.CreateDelegate(typeof(Action), method);
                delStr = null;
            }
        }

        public void OnBeforeSerialize()
        {
            delStr = null;
            if (del != null)
            {
                delStr = del.Method.DeclaringType.FullName + ";" + del.Method.ToString();
            }

        }
    }

}



