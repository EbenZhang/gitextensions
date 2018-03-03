using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitCommands;

namespace GitUI.RepoObjectsTree
{
    partial class RepoObjectsTree
    {
        private sealed class Nodes
        {
            public readonly Tree Tree;
            private readonly IList<Node> _nodesList = new List<Node>();

            public Nodes(Tree aTree)
            {
                Tree = aTree;
            }

            public void AddNode(Node aNode)
            {
                _nodesList.Add(aNode);
            }

            public void Clear()
            {
                _nodesList.Clear();
            }

            public void Remove(Node aNode)
            {
                _nodesList.Remove(aNode);
            }

            public IEnumerator<Node> GetEnumerator()
            {
                var e = _nodesList.GetEnumerator();
                return e;
            }

            /// <summary>
            /// Returns all nodes of a given TNode type using depth-first, pre-order method
            /// </summary>
            /// <typeparam name="TNode"></typeparam>
            /// <returns></returns>
            public IEnumerable<TNode> DepthEnumerator<TNode>() where TNode : Node
            {
                foreach (var node in this)
                {
                    if (node is TNode node1)
                    {
                        yield return node1;
                    }

                    foreach (var subnode in node.Nodes.DepthEnumerator<TNode>())
                    {
                        yield return subnode;
                    }
                }
            }

            internal void FillTreeViewNode(TreeNode aTreeViewNode)
            {
                var prevNodes = new HashSet<Node>();
                for (var i = 0; i < aTreeViewNode.Nodes.Count; i++)
                {
                    var tvNode = aTreeViewNode.Nodes[i];
                    prevNodes.Add(Node.GetNode(tvNode));
                }

                var oldNodeIdx = 0;
                foreach (var node in this)
                {
                    TreeNode tvNode;

                    if (oldNodeIdx < aTreeViewNode.Nodes.Count)
                    {
                        tvNode = aTreeViewNode.Nodes[oldNodeIdx];
                        var oldNode = Node.GetNode(tvNode);
                        if (!oldNode.Equals(node) && !prevNodes.Contains(node))
                        {
                            tvNode = aTreeViewNode.Nodes.Insert(oldNodeIdx, string.Empty);
                        }
                    }
                    else
                    {
                        tvNode = aTreeViewNode.Nodes.Add(string.Empty);
                    }

                    node.TreeViewNode = tvNode;
                    //recurse to subnodes
                    node.Nodes.FillTreeViewNode(tvNode);
                    oldNodeIdx++;
                }

                while (oldNodeIdx < aTreeViewNode.Nodes.Count)
                {
                    aTreeViewNode.Nodes.RemoveAt(oldNodeIdx);
                }

            }

            public int Count => _nodesList.Count;
        }

        private abstract class Tree
        {
            protected readonly Nodes Nodes;
            private readonly IGitUICommandsSource _uiCommandsSource;
            public GitUICommands UICommands => _uiCommandsSource.UICommands;
            protected GitModule Module => UICommands.Module;
            public TreeNode TreeViewNode { get; }
            public Action<List<string>> OnBranchesAdded;

            protected Tree(TreeNode aTreeNode, IGitUICommandsSource uiCommands)
            {
                Nodes = new Nodes(this);
                _uiCommandsSource = uiCommands;
                TreeViewNode = aTreeNode;
            }

            public Task ReloadTask(CancellationToken token)
            {
                ClearNodes();
                var task = new Task(() => LoadNodes(token), token);

                void ContinuationAction(Task t)
                {
                    TreeViewNode.TreeView.BeginUpdate();
                    try
                    {
                        FillTreeViewNode();
                    }
                    finally
                    {
                        if (TreeViewNode.TreeView.SelectedNode != null)
                        {
                            TreeViewNode.TreeView.SelectedNode.EnsureVisible();
                        }
                        else if (TreeViewNode.TreeView.Nodes.Count > 0)
                        {
                            TreeViewNode.TreeView.Nodes[0].EnsureVisible();
                        }

                        TreeViewNode.TreeView.EndUpdate();
                    }
                }

                task.ContinueWith(ContinuationAction, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
                return task;
            }

            protected abstract void LoadNodes(CancellationToken token);

            protected virtual void ClearNodes()
            {
                Nodes.Clear();
            }

            protected virtual void FillTreeViewNode()
            {
                Nodes.FillTreeViewNode(TreeViewNode);
            }

            protected void FireBranchAddedEvent(List<string> branchFullPaths)
            {
                OnBranchesAdded?.Invoke(branchFullPaths);
            }
        }

        private abstract class Node
        {
            public readonly Nodes Nodes;
            protected Tree Tree => Nodes.Tree;
            protected GitUICommands UICommands => Tree.UICommands;

            protected GitModule Module => UICommands.Module;

            protected Node(Tree aTree)
            {
                Nodes = new Nodes(aTree);
            }

            private TreeNode _treeViewNode;
            public TreeNode TreeViewNode
            {
                protected get => _treeViewNode;
                set
                {
                    _treeViewNode = value;
                    _treeViewNode.Tag = this;
                    _treeViewNode.Text = DisplayText();
                    _treeViewNode.ContextMenuStrip = GetContextMenuStrip();
                    ApplyStyle();
                }
            }

            private static readonly Dictionary<Type, ContextMenuStrip> DefaultContextMenus
                = new Dictionary<Type, ContextMenuStrip>();

            public static void RegisterContextMenu(Type aType, ContextMenuStrip aMenu)
            {
                if (DefaultContextMenus.ContainsKey(aType))
                {
                    // the translation unit test may create the RepoObjectTree multiple times,
                    // which results in a duplicate key exception.
                    return;
                }
                DefaultContextMenus.Add(aType, aMenu);
            }

            protected virtual ContextMenuStrip GetContextMenuStrip()
            {
                DefaultContextMenus.TryGetValue(GetType(), out var result);
                return result;
            }

            protected IWin32Window ParentWindow()
            {
                return TreeViewNode.TreeView.FindForm();
            }

            public virtual string DisplayText()
            {
                return ToString();
            }

            protected virtual void ApplyStyle()
            {
                TreeViewNode.NodeFont = AppSettings.Font;
            }

            internal virtual void OnSelected() { }
            internal virtual void OnClick() { }
            internal virtual void OnDoubleClick() { }

            public static Node GetNode(TreeNode treeNode)
            {
                return (Node)treeNode.Tag;
            }

            public static T GetNodeSafe<T>(TreeNode treeNode) where T : Node
            {
                return treeNode?.Tag as T;
            }

            public static void OnNode<T>(TreeNode treeNode, Action<T> action) where T : Node
            {
                var node = GetNodeSafe<T>(treeNode);
                if (node == null) return;
                action(node);
            }
        }
    }
}
