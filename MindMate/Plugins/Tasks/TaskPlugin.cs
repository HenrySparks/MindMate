﻿using MindMate.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MindMate.Plugins.Tasks
{
    public class TaskPlugin : IPlugin, IPluginMapNodeContextMenu
    {
        public const string ATT_DUE_DATE = "Due Date";

        private DateTimePicker dateTimePicker; 
        private TasksList taskList;
        private MenuItem[] menuItems;

        public void Initialize(IPluginManager pluginMgr)
        {
            dateTimePicker = new DateTimePicker();
            taskList = new TasksList();
            taskList.TaskViewEvent += taskList_TaskViewEvent;
        }

        void taskList_TaskViewEvent(TaskView tv, TaskView.TaskViewEvent e)
        {
            if(e == TaskView.TaskViewEvent.Remove)
            {
                tv.MapNode.DeleteAttribute(ATT_DUE_DATE);
            }
        }

        public MenuItem[] CreateContextMenuItemsForNode()
        {
            var t1 = new MenuItem("Set Due Date ...", TaskRes.date_add, SetDueDate_Click);

            var t2 = new MenuItem("Update Due Date ...", TaskRes.date_edit, SetDueDate_Click);

            var t3 = new MenuItem("Quick Due Dates");
 
            t3.AddDropDownItem(new MenuItem("Today"));
            t3.AddDropDownItem(new MenuItem("Tomorrow"));
            t3.AddDropDownItem(new MenuItem("This Week"));
            t3.AddDropDownItem(new MenuItem("Next Week"));
            t3.AddDropDownItem(new MenuItem("This Month"));
            t3.AddDropDownItem(new MenuItem("Next Month"));
            t3.AddDropDownItem(new MenuItem("No Date"));

            var t4 = new MenuItem("Complete Task", TaskRes.tick);

            menuItems = new MenuItem[] 
            {
                t1,
                t2,
                t3,
                t4
            };

            return menuItems;
        }

        public void OnContextMenuOpening(SelectedNodes nodes)
        {
            if(nodes.First.ContainsAttribute(ATT_DUE_DATE))
            {
                menuItems[0].Visible = false;
                menuItems[1].Visible = true;
            }
            else
            {
                menuItems[0].Visible = true;
                menuItems[1].Visible = false;
            }
        }

        
        /// <summary>
        /// Should only update the model, all interested views should be updated through the event generated by the model.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="nodes"></param>
        private void SetDueDate_Click(MenuItem menu, SelectedNodes nodes)
        {

            MapTree.AttributeSpec aspec = nodes.First.Tree.GetAttributeSpec(ATT_DUE_DATE);
            if (aspec == null)
                aspec = CreateDueDateAttributeSpec(nodes.First.Tree);

            MapNode.Attribute att;
            if (nodes.First.GetAttribute(aspec, out att))
            {
                dateTimePicker.Value = DateHelper.ToDateTime(att.value);
            }
            else
            {
                dateTimePicker.Value = DateTime.Today.Date.AddHours(7);
            }

            if(dateTimePicker.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    MapNode node = nodes[i];

                    if (!node.GetAttribute(aspec, out att))
                        att.AttributeSpec = aspec;

                    att.value = dateTimePicker.Value.ToString();
                    node.AddUpdateAttribute(att);

                }
            }
        }
                
        private MapTree.AttributeSpec CreateDueDateAttributeSpec(MapTree tree)
        {
            return new MapTree.AttributeSpec(
                tree, ATT_DUE_DATE, true, MapTree.AttributeDataType.DateTime, 
                MapTree.AttributeListOption.NoList, null, MapTree.AttributeType.System);
        }
                        
        public void CreateMainMenuItems(out MenuItem[] menuItems, out MainMenuLocation position)
        {
            throw new NotImplementedException();
        }

        public Control[] CreateSideBarWindows()
        {
            taskList.Text = "Tasks";
            return new Control [] { taskList };
        }

        public void OnCreatingTree(Model.MapTree tree)
        {
            tree.AttributeChanged += tree_AttributeChanged;
        }

        void tree_AttributeChanged(MapNode node, AttributeChangeEventArgs e)
        {
            if(e.ChangeType == AttributeChange.Removed)
            {
                TaskView tv = taskList.FindTaskView(node, DateHelper.ToDateTime(e.oldValue.value));
                if (tv != null) taskList.RemoveTask(tv);
            }
            else if(e.ChangeType == AttributeChange.Added)
            {
                taskList.Add(node, DateHelper.ToDateTime(e.newValue.value));
            }
            else if(e.ChangeType == AttributeChange.ValueUpdated)
            {
                TaskView tv = taskList.FindTaskView(node, DateHelper.ToDateTime(e.oldValue.value));
                if (tv != null) taskList.RemoveTask(tv);
                taskList.Add(node, DateHelper.ToDateTime(e.newValue.value));
            }

        }
                
        public void OnDeletingTree(Model.MapTree tree)
        {
            throw new NotImplementedException();
        }
                       
        
    }
}
