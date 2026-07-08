namespace DsaThreading;

public static class Searches
{

    // Linear Search: O(n) - walk the array until we find what we want. 
    // Sorted or unsorted doesn't really matter, unsorted OK
    public static int LinearSearch(int[] data, int target)
    {   
        // We could probably use a foreach but that is itself an abstraction
        for(int i = 0; i < data.Length; i++)
        {
            if (data[i] == target) return i;
        }
        // if we don't find it return -1
        return -1;
    }

    // Binary Search - halve the search space each step
    // O(log n) - but we must be sorted before we start 
    public static int BinarySearch(int[] sorted, int target)
    {
        //something like this for a recursive method
        
        // int pivot = sorted[sorted.Length/2];
        // if (pivot == target)
        // {
        //     return ;
        // }
        // else if (pivot > target) // target is in the first section
        // {
        //     sorted[..pivot]
        // }
        // else // target is on the second section
        // {
            
        // // }
        // int index = sorted.Length/2;
        // int mid = sorted[index];
        // int left = 0;
        // int right = sorted.Length;

        // if (mid == target)
        // {
        //     return index;
        // } else if (mid < sorted[right]) // target is on the second section
        // {
        //     right = index;
        //     index = right + (left-right)/2;
        // } 
        // else // target is on the first section
        // {
        //  
        //}


        // My solution :D - Jesus
        int i = sorted.Length/2, pivot = 0, size = sorted.Length/2;

        while(size > 0)
        {
            size = size / 2;
            if(sorted[i + pivot] < target)
            {
                pivot = i;
                
                i = i + size;
                
            }
            else if(sorted[i + pivot] > target)  i = size;
            else return i + pivot;
        }
        return -1;

        
    }

}