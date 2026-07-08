using System.ComponentModel;

namespace DsaThreading;

public static class Sorts
{
    // Bubble sort (O(n^2)) - we just swap adjacent pairs until the largest ones bubble to the end
    public static int[] Bubble(int[] input)
    {
        int swap;
        for (int position1 = 0; position1 < input.Length; position1++)
        {
            for(int position2 = 0; position2 < input.Length; position2++)
            {
                if(input[position1] > input[position2])
                {
                    swap = input[position1];
                    input[position1] = input[position2];
                    input[position2] = swap;

                }
            }
        }
        return input;
    }

    // Inserion Sort (O(n^2)): Building the sorted array one element at a time
    // Start with a new empty array, and the as we insert compare, and continue
    public static int[] Insertion(int[] input)
    {
        int Length = input.Length;

        // We need a for loop, and we'll start from the second element
        for (int position = 1; position < Length; position ++)
        {
            int key = input[position];
            int j = position - 1;
            // Shift elements of input that are greater than the key position ahead
            // of where they are now
            while(j >= 0 && input[j] > key)
            {
                input[j + 1] = input[j];
                j--;
            }
            // Insert the key into its sorted position
            input[j + 1] = key;
        }
        return input;
    }

    public static int[] Selection(int[] input)
    {
        int length = input.Length;

        for(int position1 = 0; position1 < length; position1++)
        {
            // Assume the current position holds the min
            int min_index = position1;

            // Iterate through the unsorted portion of the actual minimum
            for (int position2 = position1; position2 < length; position2++)
            {
                if(input[position2] < input[min_index])
                {
                    // Update the min_index 
                    min_index = position2;
                }
            }
            //move the minimum element to its correct position
            int temp = input[position1];
            input[position1] = input[min_index];
            input[min_index] = temp;
        }
        return input;
    }


    public static int[] Merge(int[] input)
    {
        // Base case, if its an array of 1
        if (input.Length <= 1) return input;

        int mid = input.Length / 2;

        // split array into two halves
        int[] left = Merge(input[..mid]);
        int[] right = Merge(input[mid..]);

        return MergeTwo(left,right);
    }

    public static int[] MergeTwo(int[] left, int[] right)
    {
        // Empty array that is the total length of left + right
        int[] sorted = new int[left.Length + right.Length];

        // Pointers
        // leftindex for left index
        // rightIndex for right index
        // sortedIndex for sorted index
        int leftindex = 0, rightIndex = 0, sortedIndex = 0;

        while(leftindex< left.Length && rightIndex < right.Length)
        {
            sorted[sortedIndex++] = left[leftindex] <= right[rightIndex] ? left[leftindex++] : right[rightIndex++];

        }
        while (leftindex < left.Length) sorted[sortedIndex ++] = left[leftindex++];

        while (rightIndex < right.Length) sorted[sortedIndex++] = right[rightIndex++];
        
        return sorted;
    } 

}