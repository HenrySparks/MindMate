﻿using MindMate.MetaModel;
using MindMate.Model;
using MindMate.Plugins.Tasks.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MindMate.Plugins.Tasks
{
    public partial class TaskPlugin : IPlugin, IPluginMainMenu
    {

        private PendingTaskList pendingTasks;
        public PendingTaskList PendingTasks { get { return pendingTasks; } }

        private CompletedTaskList completedTasks;

        /// <summary>
        /// List of all tasks (completed + pending)
        /// </summary>
        public TaskList AllTasks { get; private set; }

        private DateTimePicker dateTimePicker;

        private TaskListView taskListView;
        public TaskListView TaskListView { get { return taskListView; } }

        private IPluginManager pluginManager;
        public IPluginManager PluginManager { get { return pluginManager; } }

        

        public void Initialize(IPluginManager pluginMgr)
        {
            this.pluginManager = pluginMgr;

            pendingTasks = new PendingTaskList();
            completedTasks = new CompletedTaskList();
            AllTasks = new TaskList(pendingTasks, completedTasks);
            pendingTasks.TaskChanged += PendingTasks_TaskChanged;
            pendingTasks.TaskTextChanged += PendingTasks_TaskTextChanged;
            pendingTasks.TaskSelectionChanged += PendingTasks_TaskSelectionChanged;

            dateTimePicker = new DateTimePicker();
            taskListView = new TaskListView();
            taskListView.TaskViewEvent += OnTaskViewEvent;

            pluginMgr.ScheduleTask(new TaskSchedular.RecurringTask(
                () =>
                {
                    taskListView.Invoke((Action)RefreshTaskListView);
                },
                DateTime.Today.AddDays(1),
                TimeSpan.FromDays(1)
                )
            );                        
        }

        public void OnApplicationReady()
        {
            new Reminder.ReminderCtrl(this);
        }
                                               
        public MainMenuItem[] CreateMainMenuItems()
        {
            var mTasks = new MainMenuItem("Tasks");
            mTasks.MainMenuLocation = MainMenuLocation.Separate;

            var mCalendar = new MainMenuItem("Calendar");
            mCalendar.Click = OnCalendarMenuClick;
            mTasks.AddDropDownItem(mCalendar);

            return new MainMenuItem[] { mTasks };            
        }

        private void OnCalendarMenuClick(MenuItem m, SelectedNodes selectedNodes)
        {
            Calender.MindMateCalendar frmCalendar = new Calender.MindMateCalendar(this);
            frmCalendar.Show();
        }

        public Control[] CreateSideBarWindows()
        {
            taskListView.Text = "Tasks";
            return new Control [] { taskListView };
        }

        public void OnCreatingTree(MapTree tree)
        {
            pendingTasks.RegisterMap(tree);
            completedTasks.RegisterMap(tree);

            tree.AttributeChanged += Task.OnAttributeChanged;
        }

        public void OnDeletingTree(MapTree tree)
        {
            pendingTasks.UnregisterMap(tree);
            completedTasks.UnregisterMap(tree);

            tree.AttributeChanged += Task.OnAttributeChanged;
        }  

        /// <summary>
        /// Should only update the model, all interested views should be updated through the event generated by the model.
        /// </summary>
        /// <param name="nodes"></param>
        public void SetDueDateThroughPicker(IEnumerable<MapNode> nodes)
        {
            DateTime value;

            // initialize date time picker
            MapNode temp = nodes.First();
            if (temp != null && temp.DueDateExists())
            {
                value = ShowDueDatePicker(temp.GetDueDate());
            }
            else
            {
                value = ShowDueDatePicker();
            }
            
            // show and set due dates
            if (value != DateTime.MinValue)
            {
                foreach(MapNode node in nodes)
                {
                    node.AddTask(dateTimePicker.Value);
                }                
            }
        }

        /// <summary>
        /// Returns DateTime.MinValue if nothing is selected
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns>Returns DateTime.MinValue if nothing is selected</returns>
        public DateTime ShowDueDatePicker()
        {
            return ShowDueDatePicker(DateHelper.GetDefaultDueDate());
        }

        /// <summary>
        /// Returns DateTime.MinValue if nothing is selected
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns>Returns DateTime.MinValue if nothing is selected</returns>
        public DateTime ShowDueDatePicker(DateTime defaultValue)
        {
            dateTimePicker.Value = defaultValue;
            if (dateTimePicker.ShowDialog() == DialogResult.OK)
                return dateTimePicker.Value;
            else
                return DateTime.MinValue;
        }

        public void SetDueDateToday(MapNode node)
        {
            SetDueDateKeepTimePart(node, DateHelper.GetDefaultDueDateToday());
        }

        public void SetDueDateTomorrow(MapNode node)
        {
            SetDueDateKeepTimePart(node, DateHelper.GetDefaultDueDateTomorrow());
        }

        public void SetDueDateNextWeek(MapNode node)
        {
            SetDueDateKeepTimePart(node, DateHelper.GetDefaultDueDateNextWeek());
        }

        public void SetDueDateNextMonth(MapNode node)
        {
            SetDueDateKeepTimePart(node, DateHelper.GetDefaultDueDateNextMonth());
        }

        public void SetDueDateNextQuarter(MapNode node)
        {
            SetDueDateKeepTimePart(node, DateHelper.GetDefaultDueDateNextQuarter());
        }

        /// <summary>
        /// Sets the date component of DueDate. Time component is set if it is empty, otherwise left unchanged.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dueDate"></param>
        private void SetDueDateKeepTimePart(MapNode node, DateTime dueDate)
        {
            if (node.DueDateExists())
                dueDate = dueDate.Date.Add(node.GetDueDate().TimeOfDay);
            node.AddTask(dueDate);
        }

        /// <summary>
        /// Create a child MapNode and adds task to it
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        public bool AddSubTask(string text, DateTime startDate, DateTime endDate)
        {
            if(pluginManager.ActiveNodes.Count == 1)
            {
                MapNode node = new MapNode(pluginManager.ActiveNodes.First, text);
                node.SetStartDate(startDate);
                node.SetEndDate(endDate);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
