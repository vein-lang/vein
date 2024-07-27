namespace veinc_test.Features.Cases;

public class ListCase : TestContext
{
    [Test]
    public void ParseBlock()
    {
        Syntax.Block.End().ParseVein(
            """
            {
                capacity = 4;
                count = 0;
                items = new Array<T>(capacity);
            }
            """);
        Syntax.Block.End().ParseVein(
            """
            {
                ensureCapacity();
                items[count] = value;
                count++;
            }
            """);
        Syntax.Block.End().ParseVein(
            """
            {
                auto index = indexOf(value);
                if (index >= 0)
                    RemoveAt(index);
            }
            """);
        Syntax.Block.End().ParseVein(
            """
            {
                for (auto i = index; i < count - 1; i++)
                    items[i] = items[i + 1];
                count--;
                items[count] = null;
            }
            """);
        Syntax.Block.End().ParseVein(
            """
            {
                if (count >= capacity)
                {
                    capacity = capacity * 2;
                    auto newItems = new Array<T>(capacity);
                    for (auto i = 0; i < count; i++)
                    {
                        newItems[i] = items[i];
                    }
                    items = newItems;
                }
            }
            """);
    }
}
