using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApp_RoleClaims_DotNet.Models;
//using WebApp_RoleClaims_DotNet.Models;

namespace WebApp_RoleClaims_DotNet.DAL
{
    public class TasksDbHelper
    {
        // Get all tasks from the db.
        public static List<Task> GetAllTasks()
        {
            RoleClaimContext db = new RoleClaimContext();
            return db.Tasks.ToList();
        }

        // Add a task to the db.
        public static void AddTask(string taskText)
        {
            RoleClaimContext db = new RoleClaimContext();
            Task newTask = new Task
            {
                Status = "NotStarted",
                TaskText = taskText
            };
            db.Tasks.Add(newTask);
            db.SaveChanges();
        }

        //Update an existing task in the db.
        public static void UpdateTask(int taskId, string status)
        {
            RoleClaimContext db = new RoleClaimContext();
            Task task = db.Tasks.Find(taskId);
            task.Status = status;
            db.SaveChanges();
        }

        //Delete a task in the db
        public static void DeleteTask(int taskId)
        {
            RoleClaimContext db = new RoleClaimContext();
            Task task = db.Tasks.Find(taskId);
            db.Tasks.Remove(task);
            db.SaveChanges();
        }
    }
}