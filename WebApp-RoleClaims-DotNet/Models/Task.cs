using System;
using System.Collections.Generic;
//The following libraries were added to this sample.
//using System.IO;
//using System.Web.Hosting;

namespace WebApp_RoleClaims_DotNet.Models
{
    public class Task
    {
        //Every Task entry has a Task, a Status, and a TaskID
        public int TaskID { get; set; }
        public string TaskText { get; set; }
        public string Status { get; set; }
    }
}