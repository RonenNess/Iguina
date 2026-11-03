
namespace Iguina.Entities
{
    /// <summary>
    /// A single node in a <see cref="TreeView"/>.
    /// Every node has a value, an optional label to show instead of the value, and child nodes.
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// Node value. Should be unique in the tree if you want to select nodes by value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Text to show for this node. If null, will show <see cref="Value"/> instead.
        /// Supports paragraph style commands.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Get the text to show for this node.
        /// </summary>
        public string DisplayText => Label ?? Value;

        /// <summary>
        /// Optional user data to attach to this node.
        /// </summary>
        public object? UserData;

        /// <summary>
        /// If set, overrides the tree view rules about whether this node can be selected.
        /// If null (default), leaves are always selectable and branches are selectable based on <see cref="TreeView.BranchesSelectable"/>.
        /// </summary>
        public bool? Selectable;

        /// <summary>
        /// Get / set if this node is expanded (ie its children are shown).
        /// </summary>
        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded != value)
                {
                    _expanded = value;
                    Tree?.NotifyNodeToggled(this);
                }
            }
        }
        bool _expanded;

        /// <summary>
        /// Parent node, or null if this is a root node or not attached to any tree.
        /// Top-level nodes of a tree view have the (hidden) tree root node as parent.
        /// </summary>
        public TreeNode? Parent { get; private set; }

        /// <summary>
        /// Child nodes.
        /// </summary>
        public IReadOnlyList<TreeNode> Children => _children;
        readonly List<TreeNode> _children = new();

        /// <summary>
        /// True if this node has children.
        /// </summary>
        public bool HasChildren => _children.Count > 0;

        // tree view this node is the root of (only set for tree view root)
        internal TreeView? _ownerTree;

        /// <summary>
        /// The tree view this node belongs to, or null if not attached to a tree view.
        /// </summary>
        public TreeView? Tree
        {
            get
            {
                var node = this;
                while (node.Parent != null) { node = node.Parent; }
                return node._ownerTree;
            }
        }

        /// <summary>
        /// Node depth in tree. Top-level nodes of a tree view have depth 0.
        /// </summary>
        public int Depth
        {
            get
            {
                int depth = -1;
                var node = this;
                while (node.Parent != null) { node = node.Parent; depth++; }
                return (node._ownerTree != null) ? depth : depth + 1;
            }
        }

        /// <summary>
        /// Create the tree node.
        /// </summary>
        /// <param name="value">Node value.</param>
        /// <param name="label">Node label, or null to show value instead.</param>
        public TreeNode(string value, string? label = null)
        {
            Value = value;
            Label = label;
        }

        /// <summary>
        /// Create a new node and add it as a child of this node.
        /// </summary>
        /// <param name="value">New node value.</param>
        /// <param name="label">New node label, or null to show value instead.</param>
        /// <returns>The newly created node.</returns>
        public TreeNode AddChild(string value, string? label = null)
        {
            return AddChild(new TreeNode(value, label));
        }

        /// <summary>
        /// Add a node as a child of this node.
        /// If the node already has a parent, it will be removed from it first.
        /// </summary>
        /// <param name="node">Node to add.</param>
        /// <param name="index">Optional index to insert node at. If null, will add it last.</param>
        /// <returns>The added node.</returns>
        public TreeNode AddChild(TreeNode node, int? index = null)
        {
            // make sure we don't create a loop
            for (var curr = this; curr != null; curr = curr.Parent)
            {
                if (curr == node) { throw new InvalidOperationException("Can't add a tree node as a child of itself or of one of its descendants!"); }
            }

            node.Remove();
            node.Parent = this;
            if (index.HasValue) { _children.Insert(index.Value, node); }
            else { _children.Add(node); }
            return node;
        }

        /// <summary>
        /// Remove a child node.
        /// </summary>
        /// <param name="node">Node to remove.</param>
        /// <returns>True if node was a child of this node and was removed.</returns>
        public bool RemoveChild(TreeNode node)
        {
            if (node.Parent != this) { return false; }
            var tree = Tree;
            _children.Remove(node);
            node.Parent = null;
            tree?.NotifyNodeRemoved(node);
            return true;
        }

        /// <summary>
        /// Remove all child nodes.
        /// </summary>
        public void ClearChildren()
        {
            while (_children.Count > 0)
            {
                RemoveChild(_children[^1]);
            }
        }

        /// <summary>
        /// Remove this node from its parent.
        /// </summary>
        public void Remove()
        {
            Parent?.RemoveChild(this);
        }

        /// <summary>
        /// Expand this node and all its descendants.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var node in Traverse()) { node.Expanded = true; }
        }

        /// <summary>
        /// Collapse this node and all its descendants.
        /// </summary>
        public void CollapseAll()
        {
            foreach (var node in Traverse()) { node.Expanded = false; }
        }

        /// <summary>
        /// Expand all the parents of this node, so it will be shown in the tree.
        /// </summary>
        public void ExpandParents()
        {
            for (var node = Parent; node != null; node = node.Parent)
            {
                node.Expanded = true;
            }
        }

        /// <summary>
        /// Check if this node is a descendant of a given node.
        /// </summary>
        public bool IsDescendantOf(TreeNode node)
        {
            for (var curr = Parent; curr != null; curr = curr.Parent)
            {
                if (curr == node) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Iterate this node and all its descendants, depth-first.
        /// </summary>
        public IEnumerable<TreeNode> Traverse()
        {
            var stack = new Stack<TreeNode>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;
                for (int i = node._children.Count - 1; i >= 0; --i)
                {
                    stack.Push(node._children[i]);
                }
            }
        }

        /// <summary>
        /// Find the first node with a given value, in this node and its descendants.
        /// </summary>
        /// <param name="value">Value to search.</param>
        /// <returns>Found node, or null if not found.</returns>
        public TreeNode? Find(string value)
        {
            return Traverse().FirstOrDefault(x => x.Value == value);
        }
    }
}
