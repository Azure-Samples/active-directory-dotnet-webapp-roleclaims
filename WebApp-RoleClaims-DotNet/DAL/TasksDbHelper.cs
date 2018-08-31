/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApp_RoleClaims_DotNet.Models;

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