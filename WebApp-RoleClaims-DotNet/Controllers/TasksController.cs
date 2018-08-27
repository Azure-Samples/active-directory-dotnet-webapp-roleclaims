using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp_RoleClaims_DotNet.DAL;

namespace WebApp_RoleClaims_DotNet.Controllers
{
    public class TasksController : Controller
    {
        /// <summary>
        /// Lists Out the Tasks stored in the database.  RBAC to editing tasks is controlled by 
        /// the View and other controller actions.  Requires the user has at least one
        /// of the application roles to view tasks.
        /// </summary>
        /// <returns>The Tasks Page.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin, Observer, Writer, Approver")]
        public ActionResult Index()
        {
            ViewBag.Message = "Tasks";
            ViewData["tasks"] = TasksDbHelper.GetAllTasks();
            return View();
        }


        /// <summary>
        /// Add a new task to the database or Update the Status of an Existing Task.  Requires that
        /// the user has a application role of Admin, Writer, or Approver, and only allows certain actions based
        /// on which role(s) the user has been granted.
        /// </summary>
        /// <param name="formCollection">The user input including task name and status.</param>
        /// <returns>A Redirect to the Tasks Page.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin, Writer, Approver")]
        public ActionResult TaskSubmit(FormCollection formCollection)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Writer"))
            {
                // Add A New task to Tasks.xml
                if (formCollection["newTask"] != null && formCollection["newTask"].Length != 0)
                    TasksDbHelper.AddTask(formCollection["newTask"]);
            }

            if (User.IsInRole("Admin") || User.IsInRole("Approver"))
            {
                // Change status of existing task
                foreach (string key in formCollection.Keys)
                {
                    if (key != "newtask" && key != "delete")
                        TasksDbHelper.UpdateTask(Convert.ToInt32(key), formCollection[key]);
                }
            }

            if (User.IsInRole("Admin"))
            {
                // Delete a Task
                foreach (string key in formCollection.Keys)
                {
                    if (key == "delete" && formCollection[key] != null && formCollection[key].Length > 0)
                    {
                        string[] toDelete = formCollection[key].Split(',');
                        foreach (string id in toDelete)
                        {
                            TasksDbHelper.DeleteTask(Convert.ToInt32(id));
                        }
                    }
                }
            }
            return RedirectToAction("Index", "Tasks");
        }
    }
}