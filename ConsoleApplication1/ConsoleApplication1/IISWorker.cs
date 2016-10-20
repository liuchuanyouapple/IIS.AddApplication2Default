﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class IISWorker
    {
        private static string HostName = "localhost";

        /// <summary>
        /// 获取本地IIS版本
        /// </summary>
        /// <returns></returns>
        public static string GetIIsVersion()
        {
            try
            {
                DirectoryEntry entry = new DirectoryEntry("IIS://" + HostName + "/W3SVC/INFO");
                string version = entry.Properties["MajorIISVersionNumber"].Value.ToString();
                return version;
            }
            catch (Exception se)
            {
                //说明一点:IIS5.0中没有(int)entry.Properties["MajorIISVersionNumber"].Value;属性，将抛出异常 证明版本为 5.0
                return string.Empty;
            }
        }

        /// <summary>
        /// 创建虚拟目录网站
        /// </summary>
        /// <param name="webSiteName">网站名称</param>
        /// <param name="physicalPath">物理路径</param>
        /// <param name="domainPort">站点+端口，如192.168.1.23:90</param>
        /// <param name="isCreateAppPool">是否创建新的应用程序池</param>
        /// <returns></returns>
        public static int CreateWebSite(string webSiteName, string physicalPath, string domainPort, bool isCreateAppPool)
        {
            DirectoryEntry root = new DirectoryEntry("IIS://" + HostName + "/W3SVC");
            // 为新WEB站点查找一个未使用的ID
            int siteID = 1;
            foreach (DirectoryEntry e in root.Children)
            {
                if (e.SchemaClassName == "IIsWebServer")
                {
                    int ID = Convert.ToInt32(e.Name);
                    if (ID >= siteID) { siteID = ID + 1; }
                }
            }
            // 创建WEB站点
            DirectoryEntry site = (DirectoryEntry)root.Invoke("Create", "IIsWebServer", siteID);
            site.Invoke("Put", "ServerComment", webSiteName);
            site.Invoke("Put", "KeyType", "IIsWebServer");
            site.Invoke("Put", "ServerBindings", domainPort + ":");
            site.Invoke("Put", "ServerState", 2);
            site.Invoke("Put", "FrontPageWeb", 1);
            site.Invoke("Put", "DefaultDoc", "Default.html");
            // site.Invoke("Put", "SecureBindings", ":443:");
            site.Invoke("Put", "ServerAutoStart", 1);
            site.Invoke("Put", "ServerSize", 1);
            site.Invoke("SetInfo");
            // 创建应用程序虚拟目录

            DirectoryEntry siteVDir = site.Children.Add("Root", "IISWebVirtualDir");
            siteVDir.Properties["AppIsolated"][0] = 2;
            siteVDir.Properties["Path"][0] = physicalPath;
            siteVDir.Properties["AccessFlags"][0] = 513;
            siteVDir.Properties["FrontPageWeb"][0] = 1;
            siteVDir.Properties["AppRoot"][0] = "LM/W3SVC/" + siteID + "/Root";
            siteVDir.Properties["AppFriendlyName"][0] = "Root";

            if (isCreateAppPool)
            {
                DirectoryEntry apppools = new DirectoryEntry("IIS://" + HostName + "/W3SVC/AppPools");

                DirectoryEntry newpool = apppools.Children.Add(webSiteName, "IIsApplicationPool");
                newpool.Properties["AppPoolIdentityType"][0] = "4"; //4
                newpool.Properties["ManagedPipelineMode"][0] = "0"; //0:集成模式 1:经典模式
                newpool.CommitChanges();
                siteVDir.Properties["AppPoolId"][0] = webSiteName;
            }

            siteVDir.CommitChanges();
            site.CommitChanges();
            return siteID;
        }

        public static void CreateVirtualDir(string virtualdirname, string logicDir, bool isCreateAppPool)
        {
        
            DirectoryEntry rootEntry;
            rootEntry = new DirectoryEntry("IIS://localhost/W3SVC/1/root");

            DirectoryEntry newVirDir;
            newVirDir = rootEntry.Children.Add(virtualdirname, "IIsWebVirtualDir");
            newVirDir.Invoke("AppCreate", true);

            newVirDir.CommitChanges();
            rootEntry.CommitChanges();

            newVirDir.Properties["AnonymousPasswordSync"][0] = true;
            newVirDir.Properties["Path"][0] = logicDir;//+ @"virtualdirentry/virtualname/";

            //设置的端口绑定数据
            //_newVirDir.Properties["ServerBindings"].Value =AppEntitys.WebAppInfor.HostIp+AppEntitys.WebAppInfor.HostProt+AppEntitys.WebAppInfor.AppDesc;

            //设置起始默认页:
            newVirDir.Properties["EnableDefaultDoc"][0] = true;
            newVirDir.Properties["DefaultDoc"][0] = "Default.aspx";

            if (isCreateAppPool)
            {
                DirectoryEntry apppools = new DirectoryEntry("IIS://" + HostName + "/W3SVC/AppPools");

                DirectoryEntry newpool = apppools.Children.Add(virtualdirname, "IIsApplicationPool");
                newpool.Properties["AppPoolIdentityType"][0] = "4"; //4
                newpool.Properties["ManagedPipelineMode"][0] = "0"; //0:集成模式 1:经典模式
                newpool.CommitChanges();
                newVirDir.Properties["AppPoolId"][0] = virtualdirname;
            }

            //_newVirDir
            newVirDir.CommitChanges();
        }

        /// <summary>
        /// 得到网站的物理路径
        /// </summary>
        /// <param name="rootEntry">网站节点</param>
        /// <returns></returns>
        public static string GetWebsitePhysicalPath(DirectoryEntry rootEntry)
        {
            string physicalPath = "";
            foreach (DirectoryEntry childEntry in rootEntry.Children)
            {
                if ((childEntry.SchemaClassName == "IIsWebVirtualDir") && (childEntry.Name.ToLower() == "root"))
                {
                    if (childEntry.Properties["Path"].Value != null)
                    {
                        physicalPath = childEntry.Properties["Path"].Value.ToString();
                    }
                    else
                    {
                        physicalPath = "";
                    }
                }
            }
            return physicalPath;
        }

        /// <summary>
        /// 获取站点名
        /// </summary>
        public static List<IISInfo> GetServerBindings()
        {
            List<IISInfo> iisList = new List<IISInfo>();
            string entPath = String.Format("IIS://{0}/w3svc", HostName);
            DirectoryEntry ent = new DirectoryEntry(entPath);
            foreach (DirectoryEntry child in ent.Children)
            {
                if (child.SchemaClassName.Equals("IIsWebServer", StringComparison.OrdinalIgnoreCase))
                {
                    if (child.Properties["ServerBindings"].Value != null)
                    {
                        object objectArr = child.Properties["ServerBindings"].Value;
                        string serverBindingStr = string.Empty;
                        if (IsArray(objectArr))//如果有多个绑定站点时
                        {
                            object[] objectToArr = (object[])objectArr;
                            serverBindingStr = objectToArr[0].ToString();
                        }
                        else//只有一个绑定站点
                        {
                            serverBindingStr = child.Properties["ServerBindings"].Value.ToString();
                        }
                        IISInfo iisInfo = new IISInfo();
                        iisInfo.DomainPort = serverBindingStr;
                        iisInfo.AppPool = child.Properties["AppPoolId"].Value.ToString();//应用程序池
                        iisList.Add(iisInfo);
                    }
                }
            }
            return iisList;
        }


        public static bool CreateAppPool(string appPoolName, string Username, string Password)
        {
            bool issucess = false;
            try
            {
                //创建一个新程序池
                DirectoryEntry newpool;
                DirectoryEntry apppools = new DirectoryEntry("IIS://" + HostName + "/W3SVC/AppPools");
                newpool = apppools.Children.Add(appPoolName, "IIsApplicationPool");

                //设置属性 访问用户名和密码 一般采取默认方式
                newpool.Properties["WAMUserName"][0] = Username;
                newpool.Properties["WAMUserPass"][0] = Password;
                newpool.Properties["AppPoolIdentityType"][0] = "3";
                newpool.CommitChanges();
                issucess = true;
                return issucess;
            }
            catch // (Exception ex) 
            {
                return false;
            }
        }


        /// <summary>
        /// 建立程序池后关联相应应用程序及虚拟目录
        /// </summary>
        public static void SetAppToPool(string appname, string poolName)
        {
            //获取目录
            DirectoryEntry getdir = new DirectoryEntry("IIS://localhost/W3SVC");
            foreach (DirectoryEntry getentity in getdir.Children)
            {
                if (getentity.SchemaClassName.Equals("IIsWebServer"))
                {
                    //设置应用程序程序池 先获得应用程序 在设定应用程序程序池
                    //第一次测试根目录
                    foreach (DirectoryEntry getchild in getentity.Children)
                    {
                        if (getchild.SchemaClassName.Equals("IIsWebVirtualDir"))
                        {
                            //找到指定的虚拟目录.
                            foreach (DirectoryEntry getsite in getchild.Children)
                            {
                                if (getsite.Name.Equals(appname))
                                {
                                    //【测试成功通过】
                                    getsite.Properties["AppPoolId"].Value = poolName;
                                    getsite.CommitChanges();
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 判断object对象是否为数组
        /// </summary>
        public static bool IsArray(object o)
        {
            return o is Array;
        }
    }
}
