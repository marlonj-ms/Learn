public class ListNode
{
    public int val;
    public ListNode next;
    public ListNode(int val = 0, ListNode next = null)
    {
        this.val = val;
        this.next = next;
    }
}


public class Solution
{

    public ListNode AddTwoNumbers(ListNode l1, ListNode l2)
    {

        int exceed10 = 0;
        int sum = 0;
        ListNode head = new ListNode(0, null);
        ListNode answer = head;

        int t1, t2 = 0;

        while (l1 != null || l2 != null || exceed10 > 0)
        {
            if (l1 == null)
                t1 = 0;
            else
            {
                t1 = l1.val;
                l1 = l1.next;
            }

            if (l2 == null)
                t2 = 0;
            else
            {
                t2 = l2.val;
                l2 = l2.next;
            }

            sum = (t1 + t2 + exceed10) % 10;
            exceed10 = (t1 + t2 + exceed10) / 10;

            head.next = new ListNode(sum, null);
            head = head.next;

        }


        answer = answer.next;


        return answer;
    }
}
void Main()
{
    ListNode t1 = new ListNode(9, null);
    ListNode t2 = new ListNode(1, new ListNode(9, new ListNode(9, new ListNode(9, new ListNode(9, new ListNode(9, new ListNode(9, new ListNode(9, new ListNode(9, new ListNode(9, null))))))))));
    Solution s1 = new Solution();
    ListNode x = s1.AddTwoNumbers(t1, t2);

}

// You can define other methods, fields, classes and namespaces here
