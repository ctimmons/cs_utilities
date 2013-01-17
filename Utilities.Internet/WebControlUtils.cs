/* See UNLICENSE.txt file for license details. */

using System;
using System.Text;
using System.Web.UI.WebControls;

namespace Utilities.Internet
{
  public static class WebControlUtils
  {
    public static String GetCheckBoxListItems(CheckBoxList checkBoxList)
    {
      String result = String.Empty;

      lock (checkBoxList.Items.SyncRoot)
      {
        foreach (ListItem item in checkBoxList.Items)
          if (item.Selected)
            result += item.Text + ";";
      }

      return result.TrimEnd(";".ToCharArray());
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

    public static void MoveItemToListBox(ListBox sourceListBox, ListBox destinationListBox)
    {
      Int32 selectedIndex = sourceListBox.SelectedIndex;

      if (selectedIndex == -1)
        return;

      destinationListBox.SelectedIndex = -1;
      destinationListBox.Items.Add(new ListItem(sourceListBox.SelectedItem.Text, sourceListBox.SelectedItem.Value));
      sourceListBox.Items.Remove(sourceListBox.SelectedItem);
      SelectNextAvailableItemInListBox(sourceListBox, selectedIndex);
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
      var result = new StringBuilder();

      for (Int32 i = 0; i < listBox.Items.Count; i++)
        result.Append(listBox.Items[i].Text + "," + listBox.Items[i].Value + ";");

      return result.ToString().Trim(";".ToCharArray());
    }

    public static void PopulateListBoxFromDelimitedString(ListBox listBox, String s)
    {
      foreach (String item in s.Split(";".ToCharArray()))
      {
        String[] parts = item.Split(",".ToCharArray());
        listBox.Items.Add(new ListItem(parts[0], parts[1]));
      }
    }
  }
}
