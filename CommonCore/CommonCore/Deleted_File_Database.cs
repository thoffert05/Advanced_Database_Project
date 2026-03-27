using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Windows.Forms;

namespace CommonCore
{
    internal class Deleted_File_Database
    {
        static SortedList<string, string> deleted_to_Origional_Path_list = new SortedList<string, string>();
        public static void Read_List(string path = "Deleted_File_Directory.xml")
        {
            
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(path);
                deleted_to_Origional_Path_list.Clear();
                XmlNode Root = doc.FirstChild;
                XmlNode Node = Root.FirstChild;
                string network_path="", deleted_location="";
                while (Node != null)
                {
                    for(int i=0;i<Node.Attributes.Count;i++)
                    {
                        switch(Node.Attributes[i].Name.ToUpper())
                        {
                            case "NETWORK_PATH":
                                network_path = Node.Attributes[i].Value;
                                break;
                            case "DELETED_PATH":
                                deleted_location = Node.Attributes[i].Value;
                                break;
                        }
                    }
                    deleted_to_Origional_Path_list.Add(deleted_location, network_path);
                    Node = Node.NextSibling;
                }
                }
            catch
            {

            }
        }
        public static string Delete_File(string origional_Network_path,bool useRecyleBin,bool suppress_warnings)
        {
            string file_name = origional_Network_path.Substring(origional_Network_path.LastIndexOf('\\'));
            string local_path = CommonCore.Temp_Delete_Me_Path + file_name;
            CommonCore.Safe_Copy(origional_Network_path, local_path, true,true, useRecyleBin, suppress_warnings);
            if (useRecyleBin)
                CommonCore.DeleteFileToRecycleBin(origional_Network_path);
            else
                Safe_Delete_File(origional_Network_path);
            if (!deleted_to_Origional_Path_list.ContainsKey(local_path))
                deleted_to_Origional_Path_list.Add(local_path, origional_Network_path);
            Write_List();
            return local_path;
        }

        private static bool Safe_Delete_File(string origional_Network_path)
        {
            bool deleted = false;
            while (!deleted)
            {
                try
                {
                    
                        File.Delete(origional_Network_path);
                    
                    deleted = true;
                    return true;
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show("Failed to delete file: \"" + origional_Network_path + "\"\r\nDo you wish to try again?\r\n" + ex.Message, "", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return false;
                    }
                }

            }
            return true;
        }

        public static string Restore_File(string deleted_path,bool suppress_warnings)
        {
            string dest = ""; 
            if(deleted_to_Origional_Path_list.TryGetValue(deleted_path,out dest))
            {
                CommonCore.Safe_Copy(deleted_path, dest, true,true, false, suppress_warnings);
                deleted_to_Origional_Path_list.Remove(deleted_path);
                Write_List();
                return dest;
            }

            return "NO_FILE";
        }
        public static void Send_File_To_Recycle_Bin(string deleted_path)
        {
            CommonCore.Safe_Delete_To_Recycle_Bin(deleted_path);
            deleted_to_Origional_Path_list.Remove(deleted_path);
            Write_List();
          
        }
        public static void Write_List(string path="Deleted_File_Directory.xml")
        {
            XmlDocument doc = new XmlDocument();
            XmlElement Deleted_File_List = doc.CreateElement("Deleted_File_List");
            XmlElement Deleted_File;
            foreach(KeyValuePair<string,string>kvp in deleted_to_Origional_Path_list)
            {
                Deleted_File = doc.CreateElement("Deleted_File");
                Deleted_File.SetAttribute("Network_Path", kvp.Value);
                Deleted_File.SetAttribute("Deleted_Path", kvp.Key);
                Deleted_File_List.AppendChild(Deleted_File);
            }
            doc.AppendChild(Deleted_File_List);
            doc.Save(path);
        }

        internal static bool Can_Files_Be_Resotred(string[] files)
        {
            for(int i=0;i<files.Length;i++)
            {
                if (!deleted_to_Origional_Path_list.ContainsKey(files[i]))
                    return false;
            }

            return true;
        }
    }
}
