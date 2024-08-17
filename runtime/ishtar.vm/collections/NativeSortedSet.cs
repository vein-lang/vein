namespace ishtar.collections;

using static IshtarMath;
public unsafe interface INativeComparer<T> where T : unmanaged, IEq<T>
{
    static abstract int Compare(T* p1, T* p2);
}
public unsafe struct NativeSortedSet<T>(AllocatorBlock allocator) : IDisposable
    where T : unmanaged, IEq<T>, INativeComparer<T>
{
    private Node* root = null;
    private readonly AllocatorBlock _allocator = allocator;
    private int count = 0;


    public int Count => count;

    private struct Node
    {
        public T* value;
        public Node* left;
        public Node* right;
        public int height;
    }

    public static NativeSortedSet<T>* Create(AllocatorBlock allocator)
    {
        var set = (NativeSortedSet<T>*)allocator.alloc((uint)sizeof(NativeSortedSet<T>));
        *set = new NativeSortedSet<T>(allocator);
        return set;
    }

    public static void Free(NativeSortedSet<T>* set)
    {
        if (set is null) return;
        var allocator = set->_allocator;
        set->Dispose();
        allocator.free(set);
    }

    public void add(T* value)
    {
        root = add_node(root, value);
        count++;
    }

    public void remove(T* value) => root = RemoveNode(root, value);

    public T* min()
    {
        if (root == null)
            throw new InvalidOperationException("Set is empty.");

        return min_node(root)->value;
    }

    private Node* add_node(Node* node, T* value)
    {
        if (node == null)
        {
            var newNode = (Node*)_allocator.alloc((uint)sizeof(Node));
            newNode->value = value;
            newNode->left = null;
            newNode->right = null;
            newNode->height = 1;
            return newNode;
        }
        
        var cmp = T.Compare(value, node->value);

        switch (cmp)
        {
            case < 0:
                node->left = add_node(node->left, value);
                break;
            case > 0:
                node->right = add_node(node->right, value);
                break;
            default:
                return node;
        }

        node->height = 1 + max(get_height(node->left), get_height(node->right));

        return balance(node);
    }

    private Node* RemoveNode(Node* node, T* value)
    {
        if (node == null)
            return null;

        var cmp =  T.Compare(value, node->value);
        switch (cmp)
        {
            case < 0:
                node->left = RemoveNode(node->left, value);
                break;
            case > 0:
                node->right = RemoveNode(node->right, value);
                break;
            default:
            {
                if (node->left == null || node->right == null)
                {
                    var temp = node->left == null ? node->right : node->left;

                    if (temp == null)
                    {
                        temp = node;
                        node = null;
                    }
                    else
                        *node = *temp;

                    _allocator.free(temp);
                    count--;
                }
                else
                {
                    var temp = min_node(node->right);
                    node->value = temp->value;
                    node->right = RemoveNode(node->right, temp->value);
                }

                break;
            }
        }

        if (node == null)
            return null;

        node->height = 1 + Math.Max(get_height(node->left), get_height(node->right));

        return balance(node);
    }
    private Node* min_node(Node* node)
    {
        var current = node;
        while (current->left != null)
            current = current->left;
        return current;
    }

    private Node* balance(Node* node)
    {
        switch (get_balance(node))
        {
            case > 1:
            {
                if (get_balance(node->left) < 0)
                    node->left = rotate_left(node->left);
                return rotate_right(node);
            }
            case < -1:
            {
                if (get_balance(node->right) > 0)
                    node->right = rotate_right(node->right);
                return rotate_left(node);
            }
            default:
                return node;
        }
    }

    private Node* rotate_right(Node* z)
    {
        var x = z->left;
        var y = x->right;

        x->right = z;
        z->left = y;

        z->height = max(get_height(z->left), get_height(z->right)) + 1;
        x->height = max(get_height(x->left), get_height(x->right)) + 1;

        return x;
    }

    private Node* rotate_left(Node* w)
    {
        var y = w->right;
        var x = y->left;

        y->left = w;
        w->right = x;

        w->height = max(get_height(w->left), get_height(w->right)) + 1;
        y->height = max(get_height(y->left), get_height(y->right)) + 1;

        return y;
    }

    private int get_height(Node* node)
    {
        if (node is null) return 0;
        return node->height;
    }

    private int get_balance(Node* node)
        => node == null ? 0 : get_height(node->left) - get_height(node->right);

    public void Dispose()
    {
        clear(root);
        root = null;
        count = 0;
    }

    private void clear(Node* node)
    {
        if (node == null) return;
        clear(node->left);
        clear(node->right);
        _allocator.free(node);
    }
}
