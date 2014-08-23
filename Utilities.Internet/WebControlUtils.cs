/* See UNLICENSE.txt file for license details. */

using System;
using System.Linq;
using System.Web.UI.WebControls;

using Utilities.Core;

namespace Utilities.Internet
{
  public static class WebControlUtils
  {
    public static String GetCheckedCheckBoxListItems(CheckBoxList checkBoxList)
    {
      lock (checkBoxList.Items.SyncRoot)
      {
        return checkBoxList.Items.Cast<ListItem>().Where(li => li.Selected).Select(li => li.Text).Join(";");
      }
    }

    public static void SelectFirstItemInListBox(ListBox listBox)
    {
      listBox.SelectedIndex = (listBox.Items.Count == 0) ? -1 : 0;
    }

    private static void SelectNextAvailableItemInListBox(ListBox listBox, Int32 previousSelectedIndex)
    {
      if (listBox.Items.Count == 0)
        return;
      else if (listBox.Items.Count == previousSelectedIndex)
        listBox.SelectedIndex = listBox.Items.Count - 1;
      else
        listBox.SelectedIndex = previousSelectedIndex;
    }

    public static void MoveSelectedItemsToListBox(ListBox sourceListBox, ListBox destinationListBox)
    {
      var sourceSelectedIndex = sourceListBox.SelectedIndex;
      if (sourceSelectedIndex == -1)
        return;

      foreach (ListItem sourceListItem in sourceListBox.Items)
        if (sourceListItem.Selected)
          destinationListBox.Items.Add(new ListItem(sourceListBox.SelectedItem.Text, sourceListBox.SelectedItem.Value));

      for (var i = sourceListBox.Items.Count - 1; i >= 0; i--)
        if (sourceListBox.Items[i].Selected)
          sourceListBox.Items.Remove(sourceListBox.Items[i]);
    
      SelectNextAvailableItemInListBox(sourceListBox, sourceSelectedIndex);
    }

    public static void MoveAllItemsToListBox(ListBox sourceListBox, ListBox destinationListBox)
    {
      if (sourceListBox.Items.Count == 0)
        return;

      destinationListBox.SelectedIndex = -1;
      var a = new ListItem[sourceListBox.Items.Count];
      sourceListBox.Items.CopyTo(a, 0);
      destinationListBox.Items.AddRange((ListItem[]) a);
      sourceListBox.Items.Clear();
    }

    public static String GetListBoxItemsAsDelimitedString(ListBox listBox)
    {
      return
        listBox.Items
        .Cast<ListItem>()
        .Select(li => li.Text + "," + li.Value)
        .Join(";");
    }

    public static void PopulateListBoxFromDelimitedString(ListBox listBox, String s)
    {
      foreach (var item in s.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
      {
        var parts = item.Split(",".ToCharArray());
        listBox.Items.Add(new ListItem(parts[0], parts[1]));
      }
    }
  }
}
