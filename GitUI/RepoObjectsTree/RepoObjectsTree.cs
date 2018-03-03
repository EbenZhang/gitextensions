using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitUI.CommandsDialogs;
using ResourceManager;

namespace GitUI.RepoObjectsTree
{
    public partial class RepoObjectsTree : GitModuleControl
    {
        private readonly TranslationString _showBranchOnly =
            new TranslationString("Filter the revision grid to show this branch only\nTo show all branches, right click the revision grid, select 'view' and then the 'show all branches'");

        public FilterBranchHelper FilterBranchHelper { private get; set; }

        private readonly List<Tree> _rootNodes = new List<Tree>();
        private SearchControl<string> _txtBranchCriterion;
        private readonly HashSet<string> _branchCriterionAutoCompletionSrc = new HashSet<string>();

        public RepoObjectsTree()
        {
            InitializeComponent();
            InitiliazeSearchBox();
            treeMain.PreviewKeyDown += OnPreviewKeyDown;

            btnSearch.PreviewKeyDown += OnPreviewKeyDown;
            PreviewKeyDown += OnPreviewKeyDown;
            Translate();

            RegisterContextActions();

            treeMain.ShowNodeToolTips = true;
            treeMain.HideSelection = false;
            treeMain.NodeMouseClick += OnNodeClick;
            treeMain.NodeMouseDoubleClick += OnNodeDoubleClick;
            mnubtnFilterRemoteBranchInRevisionGrid.ToolTipText = _showBranchOnly.Text;
            mnubtnFilterLocalBranchInRevisionGrid.ToolTipText = _showBranchOnly.Text;
        }

        private void InitiliazeSearchBox()
        {
            _txtBranchCriterion = new SearchControl<string>(SearchForBranch, i => { });
            _txtBranchCriterion.OnTextEntered += () =>
            {
                OnBranchCriterionChanged(null, null);
                OnBtnSearchClicked(null, null);
            };
            _txtBranchCriterion.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _txtBranchCriterion.Name = "txtBranchCritierion";
            _txtBranchCriterion.TabIndex = 1;
            _txtBranchCriterion.TextChanged += OnBranchCriterionChanged;
            _txtBranchCriterion.KeyDown += TxtBranchCriterion_KeyDown;
            branchSearchPanel.Controls.Add(_txtBranchCriterion, 1, 0);

            _txtBranchCriterion.PreviewKeyDown += OnPreviewKeyDown;
        }

        private IList<string> SearchForBranch(string arg)
        {
            return _branchCriterionAutoCompletionSrc
                .Where(r => r.IndexOf(arg, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList();
        }

        private void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.F3 || e.KeyCode == Keys.Enter)
            {
                OnBtnSearchClicked(null, null);
            }
        }

        protected override void OnUICommandsSourceChanged(object sender, IGitUICommandsSource newSource)
        {
            base.OnUICommandsSourceChanged(sender, newSource);

            CancelBackgroundTasks();

            var localBranchesRootNode = new TreeNode(Strings.branches.Text)
            {
                ImageKey = @"LocalRepo.png",
            };
            localBranchesRootNode.SelectedImageKey = localBranchesRootNode.ImageKey;
            AddTree(new BranchTree(localBranchesRootNode, newSource));

            var remoteBranchesRootNode = new TreeNode(Strings.remotes.Text)
            {
                ImageKey = @"RemoteRepo.png",
            };
            remoteBranchesRootNode.SelectedImageKey = remoteBranchesRootNode.ImageKey;
            _remoteTree = new RemoteBranchTree(remoteBranchesRootNode, newSource)
            {
                TreeViewNode = {ContextMenuStrip = menuRemotes}
            };
            AddTree(_remoteTree);

            if (showTagsToolStripMenuItem.Checked)
            {
                AddTags();
            }
        }

        private void AddBranchesToAutoCompletionSrc(List<string> branchPaths)
        {
            foreach (var branchFullPath in branchPaths)
            {
                AddBranchToAutoCompletionSrc(branchFullPath);
            }
        }
        private void AddBranchToAutoCompletionSrc(string branchFullPath)
        {
            var lastPart = branchFullPath
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault();

            _branchCriterionAutoCompletionSrc.Add(branchFullPath);

            if (lastPart == null || lastPart == branchFullPath) return;
            if (!_branchCriterionAutoCompletionSrc.Contains(lastPart))
            {
                _branchCriterionAutoCompletionSrc.Add(lastPart);
            }
        }

        private void AddTree(Tree aTree)
        {
            aTree.OnBranchesAdded += AddBranchesToAutoCompletionSrc;
            aTree.TreeViewNode.SelectedImageKey = aTree.TreeViewNode.ImageKey;
            aTree.TreeViewNode.Tag = aTree;
            treeMain.Nodes.Add(aTree.TreeViewNode);
            _rootNodes.Add(aTree);
        }

        private CancellationTokenSource _cancelledTokenSource;
        private TreeNode _tagTreeRootNode;
        private TagTree _tagTree;
        private RemoteBranchTree _remoteTree;
        private List<TreeNode> _searchResult;
        private bool _searchCriteriaChanged;
        private Task[] _tasks;

        private void CancelBackgroundTasks()
        {
            if (_cancelledTokenSource != null)
            {
                _cancelledTokenSource.Cancel();
                _cancelledTokenSource.Dispose();
                _cancelledTokenSource = null;
                if (_tasks != null)
                {
                    Task.WaitAll(_tasks);
                }
                _branchCriterionAutoCompletionSrc.Clear();
            }
            _cancelledTokenSource = new CancellationTokenSource();
        }

        public void Reload()
        {
            CancelBackgroundTasks();
            var token = _cancelledTokenSource.Token;
            _tasks = _rootNodes.Select(r => r.ReloadTask(token)).ToArray();
            Task.Factory.ContinueWhenAll(_tasks,
                t =>
                {
                    if (t.Any(r => r.Status != TaskStatus.RanToCompletion))
                    {
                        return;
                    }
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    BeginInvoke(new Action(() =>
                    {
                        var autoCompletionSrc = new AutoCompleteStringCollection();
                        autoCompletionSrc.AddRange(
                            _branchCriterionAutoCompletionSrc.ToArray());
                    }));
                }, _cancelledTokenSource.Token);
            _tasks.ToList().ForEach(t => t.Start());
        }

        private void OnBtnSettingsClicked(object sender, EventArgs e)
        {
            btnSettings.ContextMenuStrip.Show(btnSettings, 0, btnSettings.Height);
        }

        private void ShowTagsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _searchResult = null;
            if (showTagsToolStripMenuItem.Checked)
            {
                AddTags();
                var task = _rootNodes.Last().ReloadTask(_cancelledTokenSource.Token);
                task.Start(TaskScheduler.Default);
            }
            else
            {
                _rootNodes.Remove(_tagTree);
                treeMain.Nodes.Remove(_tagTreeRootNode);
            }
        }

        private void AddTags()
        {
            _tagTreeRootNode = new TreeNode(Strings.tags.Text) {ImageKey = @"tags.png"};
            _tagTreeRootNode.SelectedImageKey = _tagTreeRootNode.ImageKey;
            _tagTree = new TagTree(_tagTreeRootNode, UICommandsSource);
            AddTree(_tagTree);
            _searchResult = null;
        }

        private void OnBtnSearchClicked(object sender, EventArgs e)
        {
            _txtBranchCriterion.CloseDropdown();
            if (_searchCriteriaChanged && _searchResult != null && _searchResult.Any())
            {
                _searchCriteriaChanged = false;
                foreach (var coloredNode in _searchResult)
                {
                    coloredNode.BackColor = SystemColors.Window;
                }
                _searchResult = null;
                if (_txtBranchCriterion.Text.IsNullOrWhiteSpace())
                {
                    _txtBranchCriterion.Focus();
                    return;
                }
            }
            if (_searchResult == null || !_searchResult.Any())
            {
                if (_txtBranchCriterion.Text.IsNotNullOrWhitespace())
                {
                    _searchResult = SearchTree(_txtBranchCriterion.Text, treeMain.Nodes);
                }
            }
            var node = GetNextSearchResult();
            if (node == null) return;
            node.EnsureVisible();
            treeMain.SelectedNode = node;
        }

        private static List<TreeNode> SearchTree(string text, TreeNodeCollection nodes)
        {
            var ret = new List<TreeNode>();
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is BaseBranchNode branch)
                {
                    if (branch.FullPath.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        AddTreeNodeToSearchResult(ret, node);
                    }
                }
                else
                {
                    if (node.Text.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        AddTreeNodeToSearchResult(ret, node);
                    }
                }
                ret.AddRange(SearchTree(text, node.Nodes));
            }
            return ret;
        }

        private static void AddTreeNodeToSearchResult(ICollection<TreeNode> ret, TreeNode node)
        {
            node.BackColor = Color.LightYellow;
            ret.Add(node);
        }

        private TreeNode GetNextSearchResult()
        {
            if (_searchResult == null || !_searchResult.Any())
            {
                return null;
            }

            var node = _searchResult.First();
            _searchResult.RemoveAt(0);
            _searchResult.Add(node);
            return node;
        }

        private void OnBranchCriterionChanged(object sender, EventArgs e)
        {
            _searchCriteriaChanged = true;
        }

        private void TxtBranchCriterion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            OnBtnSearchClicked(null, null);
            e.Handled = true;
        }

        private void OnNodeSelected(object sender, TreeViewEventArgs e)
        {
            Node.OnNode<Node>(e.Node, node => node.OnSelected());
        }

        private void OnNodeClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeMain.SelectedNode = e.Node;
            Node.OnNode<Node>(e.Node, node => node.OnClick());
        }

        private void OnNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Don't use e.Node, when folding/unfolding a node,
            // e.Node won't be the one you double clicked, but a child node instead
            Node.OnNode<Node>(treeMain.SelectedNode, node => node.OnDoubleClick());
        }
    }
}