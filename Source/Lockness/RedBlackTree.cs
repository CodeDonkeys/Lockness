using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CodeDonkeys.Lockness
{
    internal enum Color
    {
        Red,
        Black
    }

    internal enum OwnershipFlag
    {
        Free,
        Owned
    }
    
    internal enum OperationType
    {
        Search,
        Add,
        Remove
    }
    
    internal enum OperationStatus
    {
        Wating,
        InProgress,
        Completed
    }

    internal class RedBlackTreePointerNode<TElement> : AtomicMarkableReference<RedBlackTreeNode<TElement>, OwnershipFlag>
    {
        public RedBlackTreeNode<TElement> Node => (RedBlackTreeNode<TElement>) this;
        public RedBlackTreePointerNode(RedBlackTreeNode<TElement> node, OwnershipFlag flag) : base(node, flag)
        {
        }
    }

    internal class RedBlackTreeNode<TElement>
    {
        public Color Color;
        public TElement Key;
        public RedBlackTreePointerNode<TElement> Left;
        public RedBlackTreePointerNode<TElement> Right;
        public OperationRecord<TElement> OperationData;
        public bool IsLeaf;

        public RedBlackTreeNode(Color color, TElement key, RedBlackTreePointerNode<TElement> left, RedBlackTreePointerNode<TElement> right)
        {
            Color = color;
            Key = key;
            Left = left;
            Right = right;
            IsLeaf = false;
        }

        public RedBlackTreeNode(TElement key) : this(Color.Black, key, null, null)
        {
            IsLeaf = true;
        }
    }

    internal class RedBlackTreeNodeWithParent<TElement> : RedBlackTreeNode<TElement>
    {
        public RedBlackTreePointerNode<TElement> Parent;
        public RedBlackTreeNodeWithParent(Color color, TElement key, RedBlackTreePointerNode<TElement> left, RedBlackTreePointerNode<TElement> right, RedBlackTreePointerNode<TElement> parent) : base(color, key, left, right)
        {
            Parent = parent;
        }

        public RedBlackTreeNodeWithParent(TElement key, RedBlackTreePointerNode<TElement> parent) : base(key)
        {
            Parent = parent;
        }

        public RedBlackTreeNodeWithParent(RedBlackTreeNode<TElement> sample, RedBlackTreePointerNode<TElement> parent)
            :this(sample.Color, sample.Key, sample.Left, sample.Right, parent)
        { }
    }

    internal class OperationRecord<TElement>
    {
        public OperationType Type;
        public TElement Key;
        public int OperationId;
        public AtomicMarkableReference<RedBlackTreePointerNode<TElement>, OperationStatus> State;

        public OperationRecord(OperationType type, TElement key, int operationId, AtomicMarkableReference<RedBlackTreePointerNode<TElement>, OperationStatus> state)
        {
            Type = type;
            Key = key;
            OperationId = operationId;
            State = state ?? new AtomicMarkableReference<RedBlackTreePointerNode<TElement>, OperationStatus>(null, OperationStatus.Wating);
        }
    }

    public sealed class RedBlackTree<TElement> : ISet<TElement>
    {
        private readonly IComparer<TElement> comparer;
        private volatile RedBlackTreePointerNode<TElement> root;
        private int[] ST;

        public RedBlackTree(IComparer<TElement> elementComparer)
        {
            comparer = elementComparer;
            root = new RedBlackTreePointerNode<TElement>(new RedBlackTreeNode<TElement>(default(TElement)), OwnershipFlag.Free);
            ST = new int[100000];
        }

        public bool Add(TElement element)
        {
            var isFound = Contains(element);
            if (isFound)
            {
                return false;
            }
            var currentOperation = new OperationRecord<TElement>(OperationType.Add, element, 4, null);
            return ExecuteOperation(currentOperation);
        }

        public bool Remove(TElement element)
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(TElement key)
        {
            return Search(key) != null;
        }

        private RedBlackTreePointerNode<TElement> Search(TElement key)
        {
            var currentState = new AtomicMarkableReference<RedBlackTreePointerNode<TElement>, OperationStatus>(null, OperationStatus.InProgress);
            var currentOperation = new OperationRecord<TElement>(OperationType.Search, key, 4, currentState);
            Traverse(currentOperation);
            return currentOperation.State;
        }

        private void Traverse(OperationRecord<TElement> currentOperation)
        {
            var currentNode = root.Node;
            while (currentNode.IsLeaf)
            {
                OperationStatus oldStatus;
                currentOperation.State.Get(out oldStatus);
                if (oldStatus == OperationStatus.Completed)
                {
                    return;
                }
                currentNode = comparer.Compare(currentOperation.Key, currentNode.Key) < 0 ? currentNode.Left : currentNode.Right;
            }
            var isFound = comparer.Compare(currentNode.Key, currentOperation.Key) == 0;
            //TODO: возвращаем root, потому что в алгоритме коряво
            var newState = new AtomicMarkableReference<RedBlackTreePointerNode<TElement>, OperationStatus>(isFound ? root : null, OperationStatus.Completed);
            currentOperation.State = newState;
        }

        private bool ExecuteOperation(OperationRecord<TElement> operation)
        {
            InjectOperation(operation);
            OperationStatus status;
            var pointerNode = operation.State.Get(out status);
            while (status != OperationStatus.Completed)
            {
                var node = pointerNode.Node;
                if (node.OperationData == operation)
                {
                    ExecuteWindowTransaction(pointerNode, node);
                }
                pointerNode = operation.State.Get(out status);
            }
            //TODO: может можно понять, когда не true
            return true;
        }

        private void InjectOperation(OperationRecord<TElement> operation)
        {
            OperationStatus status;
            operation.State.Get(out status);
            while (status == OperationStatus.Wating)
            {
                var oldRootNode = root.Node;
                if (oldRootNode.OperationData != null)
                {
                    ExecuteWindowTransaction(root, oldRootNode);
                }
                var currentRootPointer = root;
                OwnershipFlag currentRootFlag;
                var currentRootNode = currentRootPointer.Get(out currentRootFlag);
                if (oldRootNode == currentRootNode)
                {
                    var currentNode = new RedBlackTreeNodeWithParent<TElement>(currentRootNode, null);
                    currentNode.OperationData = operation;
                    var newRootPointer = new RedBlackTreePointerNode<TElement>(currentNode, OwnershipFlag.Owned);
                    if (currentRootFlag == OwnershipFlag.Free &&
                        Interlocked.CompareExchange(ref root, newRootPointer, currentRootPointer) == currentRootPointer)
                    {
                        operation.State.CompareAndSet(newRootPointer, newRootPointer, OperationStatus.Wating, OperationStatus.InProgress);
                    }
                }
                operation.State.Get(out status);
            }
        }

        private void ExecuteWindowTransaction(RedBlackTreePointerNode<TElement> pointerNode, RedBlackTreeNode<TElement> node)
        {
            var operation = node.OperationData;
            OwnershipFlag flag;
            var currentNode = pointerNode.Get(out flag);
            if (operation == currentNode.OperationData)
            {
                if (flag == OwnershipFlag.Owned)
                {
                    var currentRoot = root;
                    if (pointerNode == currentRoot)
                    {
                        operation.State.CompareAndSet(currentRoot, currentRoot, OperationStatus.Wating, OperationStatus.InProgress);
                    }
                    if (!ExecuteCheapWindowTransaction(pointerNode, currentNode))
                    {
                        var key = operation.Key;
                        var cloneWindowRoot = new RedBlackTreeNodeWithParent<TElement>(currentNode, null);
                        var cloneWindowRootPointer = new RedBlackTreePointerNode<TElement>(cloneWindowRoot, OwnershipFlag.Free);
                        if (cloneWindowRoot.Color == Color.Red)
                        {
                            cloneWindowRoot.Color = Color.Black;
                        }

//                        CloneAndHelp(cloneWindowRoot, cloneWindowRoot.Left, ChildOrientation.Left);
//                        CloneAndHelp(cloneWindowRoot, cloneWindowRoot.Right, ChildOrientation.Right);
                        
//                        if (cloneWindowRoot.Left.Node.Color == Color.Red && cloneWindowRoot.Right.Node.Color == Color.Red)
//                        {
//                            cloneWindowRoot.Left.Node.Color = Color.Black;
//                            cloneWindowRoot.Right.Node.Color = Color.Black;
//                        }

                        var current = cloneWindowRootPointer; 
                        while (true)
                        {
                            CloneAndHelp(current, current.Node.Left, ChildOrientation.Left);
                            CloneAndHelp(current, current.Node.Right, ChildOrientation.Right);
                            
                            if (current.Node.IsLeaf)
                            {
                                OperationOnLeafNode(cloneWindowRoot, current);
                                break;
                            }
//                            else if (current.Color == Color.Black && (current.Left.Node.Color == Color.Black || current.Right.Node.Color == Color.Black))
//                            {
//                                
//                            }
                            else
                            {

                            }

                            current = (RedBlackTreeNodeWithParent<TElement>)(comparer.Compare(key, current.Key) <= 0 ? current.Left.Node : current.Right.Node);
                        }
                    }
                }
            }

        }

        private void OperationOnLeafNode(RedBlackTreeNodeWithParent<TElement> windowRootNode, RedBlackTreePointerNode<TElement> leafPointer)
        {
            var operation = windowRootNode.OperationData;
            if (operation?.Type == OperationType.Add)
            {
                var newInternalNode = new RedBlackTreeNodeWithParent<TElement>(Color.Red, operation.Key, null, null, ((RedBlackTreeNodeWithParent<TElement>)leafPointer.Node).Parent);
                var newPointerNode = new RedBlackTreePointerNode<TElement>(new RedBlackTreeNodeWithParent<TElement>(operation.Key, leafPointer), OwnershipFlag.Free);
                var newLeafPointer = new RedBlackTreePointerNode<TElement>(leafPointer.Node, OwnershipFlag.Free);
                if (comparer.Compare(operation.Key, leafPointer.Node.Key) <= 0)
                {
                    newInternalNode.Left = newPointerNode;
                    newInternalNode.Right = newLeafPointer;
                }
                else
                {
                    newInternalNode.Left = newLeafPointer;
                    newInternalNode.Right = newPointerNode;
                }
                leafPointer.Set(newInternalNode, OwnershipFlag.Free);
            }
            else
            {
                throw new NotImplementedException("Delete");
            }
        }

        public enum ChildOrientation
        {
            Left,
            Right
        }

        private void CloneAndHelp(RedBlackTreePointerNode<TElement> parentClone, RedBlackTreePointerNode<TElement> childPointer, ChildOrientation childOrientation)
        {
            if (childPointer == null)
            {
                return;
            }
            var childNode = childPointer.Node;
            if (childNode.OperationData != null)
            {
                ExecuteWindowTransaction(childPointer, childNode);
            }
            OwnershipFlag flag;
            var currentChildNode = childPointer.Get(out flag);
            var newPointer = new RedBlackTreePointerNode<TElement>(new RedBlackTreeNodeWithParent<TElement>(currentChildNode, parentClone), flag);
            if (childOrientation == ChildOrientation.Left)
            {
                parentClone.Node.Left = newPointer;
            }
            else
            {
                parentClone.Node.Right = newPointer;
            }
        }

        private bool ExecuteCheapWindowTransaction(RedBlackTreePointerNode<TElement> pointerNode, RedBlackTreeNode<TElement> currentNode)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }
    }
}