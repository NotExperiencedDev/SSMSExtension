using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Management.SqlStudio.Explorer;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System.Text.RegularExpressions;
using System.Diagnostics;



namespace Ssms2012Extender
{
    public class Connect : IDTExtensibility2
    {
        private HierarchyObject _tableMenu = null;
        private Regex _tableRegex = new Regex(@"^Server\[[^\]]*\]/Database\[[^\]]*\]/Table\[[^\]]*\]$");
        ObjectExplorerService objExplorerService;
        ObjectExplorerExtender _objectExplorerExtender;
        delegate void TrvEventAfterExpand(object obj, TreeViewEventArgs e);
        delegate void TrvEventBeforeExpand(object obj, TreeViewCancelEventArgs e);

        ContextService cs;
        TreeView _trv = null;
        private static SimpleLogger logger = null;

        /// <summary>
        /// addin constructor
        /// </summary>
        public Connect()
        {
            logger = SimpleLogger.CreateLogger(LocalHelper.LoggingEnabled, LocalHelper.LoggingPath);
            debug_message("Connect called");
        }


        /// <summary>
        /// not addin update
        /// </summary>
        /// <param name="custom"></param>
        public void OnAddInsUpdate(ref Array custom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// on shutdown
        /// </summary>
        /// <param name="custom"></param>
        public void OnBeginShutdown(ref Array custom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// main addin entry method
        /// </summary>
        /// <param name="Application"></param>
        /// <param name="ConnectMode"></param>
        /// <param name="AddInInst"></param>
        /// <param name="custom"></param>
        public void OnConnection(object Application, ext_ConnectMode ConnectMode, object AddInInst, ref Array custom)
        {
            try
            {
                /* Microsoft.SqlServer.Management.UI.VSIntegration.ServiceCache
                 * is from SqlPackageBase.dll and not from Microsoft.SqlServer.SqlTools.VSIntegration.dll
                 * the code below just throws null exception if you have wrong reference */
            
                objExplorerService = (ObjectExplorerService)ServiceCache.ServiceProvider.GetService(typeof(IObjectExplorerService));
                cs = (ContextService)objExplorerService.Container.Components[0];
                cs.ObjectExplorerContext.CurrentContextChanged += new NodesChangedEventHandler(ObjectExplorerContext_CurrentContextChanged);
            }
            catch (Exception ex)
            {
               debug_message("OnConnection::ERROR " + ex.Message);
            }

        }

        /// <summary>
        /// when the object explorer is loaded and change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void ObjectExplorerContext_CurrentContextChanged(object sender, NodesChangedEventArgs args)
        {
            debug_message("ObjectExplorerContext_CurrentContextChanged::");
            try
            {
                if (_trv == null)
                    _trv = GetObjectExplorerTreeView();

                if (_trv != null && !_trv.InvokeRequired)
                {
                    
                    if (_objectExplorerExtender == null)
                    {
                        logger.LogStart("ObjectExplorerExtender");
                        _objectExplorerExtender = new ObjectExplorerExtender();
                        logger.LogEnd("ObjectExplrerExtender");
                        if (_trv != null)
                        {
                            logger.LogStart("ImageList");
                            if (!_trv.ImageList.Images.ContainsKey("FolderSelected"))
                            {
                                _trv.ImageList.Images.Add("FolderSelected", Resources.folder);
                                _trv.ImageList.Images.Add("Table", Resources.tab_ico);
                                _trv.ImageList.Images.Add("FolderDown", Resources.folder_closed);
                                _trv.ImageList.Images.Add("FolderEdit", Resources.folder);
                                _trv.ImageList.Images.Add("FunctionFun", Resources.fun_ico);

                                _trv.BeforeExpand += new TreeViewCancelEventHandler(_trv_BeforeExpand);
                                _trv.AfterExpand += new TreeViewEventHandler(_trv_AfterExpand);
                                //TODO: _trv.NodeMouseClick += new TreeNodeMouseClickEventHandler(_trv_NodeMouseClick);
                            }
                            logger.LogEnd("ImageList");
                        }
                    }
                }
                else if (_trv != null)
                {
                    logger.LogStart(" Inovke Required sender is " + (sender == null ? "null" : "not null"), "args is " +
                             (args == null ? "null" : "not null"));
                    //IntPtr ptr = _trv.Handle;
                    _trv.BeginInvoke(new NodesChangedEventHandler(ObjectExplorerContext_CurrentContextChanged), new object[] { sender, args });
                    logger.LogEnd("Invoke Required ");
                }
            }
            catch (Exception ObjectExplorerContextException)
            {
                debug_message(String.Format("ObjectExplorerContext_CurrentContextChanged::ERROR:{0}", ObjectExplorerContextException.Message));
            }
        }

        /// <summary>
        /// When disconnected
        /// </summary>
        /// <param name="RemoveMode"></param>
        /// <param name="custom"></param>
        public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
        {
            //throw new NotImplementedException();
        }

        public void OnStartupComplete(ref Array custom)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the underlying object which is responsible for displaying object explorer structure
        /// </summary>
        /// <returns></returns>
        private TreeView GetObjectExplorerTreeView()
        {

            Type t = objExplorerService.GetType();

            //FieldInfo field = t.GetField("Tree", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = t.GetField("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null)
            {
                return (TreeView)field.GetValue(objExplorerService);
            }
            else
            {
                PropertyInfo pi = t.GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null) return (TreeView)pi.GetValue(objExplorerService, null);
                return null;
            }
        }

        /// <summary>
        /// Adds new nodes and move items between them
        /// </summary>
        /// <param name="node"></param>
        void ReorganizeFolders(TreeNode node)
        {
            logger.LogStart(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                if (node != null && node.Parent != null)
                {
                    INodeInformation ni = _objectExplorerExtender.GetNodeInformation(node);
                    if (ni != null && !string.IsNullOrEmpty(ni.UrnPath))
                    {
                        //MessageBox.Show(ni.UrnPath);
                        switch (ni.UrnPath)
                        {
                            case "Server/Database/UserTablesFolder":
                                _objectExplorerExtender.ReorganizeNodes(node, "FolderEdit", "Table");
                                break;
                            case "Server/Database/StoredProceduresFolder":
                            case "Server/Database/Table-valuedFunctionsFolder":
                            case "Server/Database/Scalar-valuedFunctionsFolder":
                                _objectExplorerExtender.ReorganizeNodes(node, "FunctionFun", string.Empty);
                                break;
                            case "Server/DatabasesFolder":
                                //TODO: _objectExplorerExtender.ReorganizeDbNodes(node, "FolderEdit", string.Empty, GetDictionary());
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    MessageBox.Show(ex.ToString());
                else
                    MessageBox.Show(ex.ToString());
            }

            logger.LogEnd(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }


        /// <summary>
        /// After expand node
        /// </summary>
        /// <param name="sender">object explorer</param>
        /// <param name="e">expanding node</param>
        void _trv_AfterExpand(object sender, TreeViewEventArgs e)
        {

            logger.LogStart(System.Reflection.MethodBase.GetCurrentMethod().Name);
            // Wait for the async node expand to finish or we could miss indexes
            try
            {
                HierarchyTreeNode htn = null;
                int cnt = 0;
                while ((htn = e.Node as HierarchyTreeNode) != null && htn.Expanding && cnt < 50000)
                {
                    Application.DoEvents();
                    cnt++;
                }
                if (_trv.InvokeRequired)
                    _trv.BeginInvoke(new TrvEventAfterExpand(_trv_AfterExpand), new object[] { sender, e });
                else
                    ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                debug_message(ex.ToString());
                debug_message(ex.StackTrace.ToString());
            }

            logger.LogEnd(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Object explorer tree view: event before expand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _trv_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {

            logger.LogStart(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                if (_trv.InvokeRequired)
                    _trv.BeginInvoke(new TrvEventBeforeExpand(_trv_BeforeExpand), new object[] { sender, e });
                else
                    ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                MessageBox.Show(ex.ToString());
            }

            logger.LogEnd(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }

        private static void debug_message(string message)
        {
#if ShowMsgBoxUserDef
            MessageBox.Show(message);
#endif
            logger.Log(message);
        }
    }
}
