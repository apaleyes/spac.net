## 5 Fancier Fork-Join Algorithms: Prefix, Pack, Sort

This section presents a few more sophisticated parallel algorithms. The intention is to demonstrate (a) sometimes problems that seem inherently sequential turn out to have efficient parallel algorithms, (b) we can use parallel-algorithm techniques as building blocks for other larger parallel algorithms, and (c) we can use asymptotic complexity to help decide when one parallel algorithm is better than another. The study of parallel algorithms could take an entire course, so we will pick just a few examples that represent some of the many key parallel-programming patterns.

As is common when studying algorithms, we will not show full C# implementations. It should be clear at this point that one could code up the  algorithms using the Task Parallel Library even if it may not be entirely easy  to implement more sophisticated techniques.

### 5.1 Parallel-Prefix Sum

Consider this problem: Given an array of _n_ integers `input`, produce an array of _n_ integers `output` where `output[i]` is the sum of the first `i` elements of `input`. In other words, we are computing the sum of _every_ prefix of the input array and returning all the results. This is called the _prefix-sum problem_ (note: It is common to distinguish the inclusive-sum (the first `i` elements) from the exclusive-sum (the first `i-1` elements); we will assume inclusive sums are desired.). (!!) Figure 8 shows an example input and output. A _Θ(n)_ sequential solution is trivial:

    int[] PrefixSum(int[] input)
    {
        int[] output = new int[input.Length];
        output[0] = input[0];
        for (int i = 1; i < input.Length; i++)
        {
            output[i] = output[i-1] + input[i];
        }

        return output;
    }

It is not at all obvious that a good parallel algorithm, say, one with _Θ(log n)_ span, exists. After all, it seems we need `output[i-1]` to compute `output[i]`. If so, the span will be _Θ(n)_. Just as a parallel reduction uses a totally different algorithm than the straightforward sequential approach, there is also an efficient parallel algorithm for the prefix-sum problem. Like many clever data structures and algorithms, it is not something most people are likely to discover on their own, but it is a useful technique to know.

The algorithm works in two passes. We will call the first pass the “up” pass because it builds a binary tree from bottom to top. We first describe the resulting tree and then explain how it can be produced via a fork-join computation. (!!) Figure 9 shows an example.

 * Every node holds the sum of the integers for some range of the `input` array.
 * The root of the tree holds the sum for the entire range [0, _n_). (note: As before, we describe ranges as including their left end but excluding their right end.)
 * A node’s left child holds the sum for the left half of the node’s range and the node’s right child holds the sum for the right half of the node’s range.  For example, the root’s left child is for the range [0, _n_/2) and the root’s right child is for the range [_n_/2, _n_).
 * Conceptually, the leaves of the tree hold the sum for one-element ranges. So there are _n_ leaves. In practice, we would use a sequential cut-off and have the leaves store the sum for a range of, say, approximately 500 elements.

To build this tree — and we do mean here to build the actual tree data-structure (note: As a side-note, if you have seen an array-based representation of a complete tree, for example with a binary-heap representation of a priority queue, then notice that _if_ the array length is a power of two, then the tree we build is also complete and therefore amenable to a compact array representation. The length of the array needs to be 2_n_ − 1 (or less with a sequential cut-off). If the array length is not a power of two and we still want a compact array, then we can either act as though the array length is the next larger power of two or use a more sophisticated rule for how to divide subranges so that we always build a complete tree.) because we need it in the second pass — we can use a straightforward fork-join computation:

 * The overall goal is to produce the node for the range [0, _n_).
 * To build the node for the range [_x_, _y_):
     - If _x == y − 1_, produce a node holding `input[x]`.
     - Else recursively in parallel build the nodes for [_x_, (_x_ + _y_) / 2) and [(_x_ + _y_) / 2, _y_). Make these the left and right children of the result node. Add their answers together for the result node’s sum.

In short, the result of the divide-and-conquer is a tree node and the way we “combine results” is to use the two recursive results as the subtrees. So we build the tree “bottom-up,” creating larger subtrees from as we return from each level of the recursion. (!!) Figure 9 shows an example of this bottom-up process, where each node stores the range it stores the sum for and the corresponding sum. The “fromleft” field is blank — we use it in the second pass.

Convince yourself this algorithm is _Θ(n)_ work and _Θ(log n)_ span.

The description above assumes no sequential cut-off. With a cut-off, we simply stop the recursion when _y_ − _x_ is below the cut-off and create one node that holds the sum of the range [_x_, _y_), computed sequentially.

Now we are ready for the second pass called the “down” pass, where we use this tree to compute the prefix-sum. The essential trick is that we process the tree from top to bottom, passing “down” as an argument the sum of the array indices to the left of the node. (!!) Figure 10 shows an example. Here are the details:

 * The argument passed to the root is 0. This is because there are no numbers to the left of the range [0, _n_) so their sum is 0.
 * The argument passed to a node’s left child is the same argument passed to the node. This is because the sum of numbers to the left of the range [_x_, (_x_ + _y_) / 2) is the sum of numbers to the left of the range [_x_, _y_).
 * The argument passed to a node’s right child is the argument passed to the node _plus_ the sum stored at the node’s left child. This is because the sum of numbers to the left of the range [(_x_ + _y_) / 2, _y_) is the sum to the left of _x_ plus the sum of the range [_x_, (_x_ + _y_) / 2). This is why we stored these sums in the up pass!

When we reach a leaf, we have exactly what we need: `output[i]` is `input[i]` plus the value passed down to the `i`th leaf. Convincing yourself this algorithm is correct will likely require working through a short example while drawing the binary tree.

This second pass is also amenable to a parallel fork-join computation. When we create a subproblem, we just need the value being passed down and the node it is being passed to. We just start with a value of 0 and the root of the tree. This pass, like the first one, is _Θ(n)_ work and _Θ(log n)_ span. So the algorithm overall is _Θ(n)_ work and _Θ(log n)_ span. It is _asymptotically_ no more expensive than computing just the sum of the whole array. The parallel-prefix problem, surprisingly, has a solution with exponential parallelism!

If we used a sequential cut-off, then we have a range of output values to produce at each leaf. The value passed down is still just what we need for  the sum of all numbers to the left of the range and then a simple sequential computation can produce all the output-values for the range at each leaf proceeding left-to-right through the range.

Perhaps the prefix-sum problem is not particularly interesting. But  just as our original sum-an-array problem exemplified the parallel-reduction pattern, the prefix-sum problem exemplifies the more general parallel-prefix pattern. Here are two other general problems that can be solved the same way as the prefix-sum problem; you can probably think of more.

 * Let `output[i]` be the minimum (or maximum) of all elements to the left of `i`.
 * Let `output[i]` be a count of how many elements to the left of `i` satisfy some property.

Moreover, many parallel algorithms for problems that are not “obviously parallel” use a parallel-prefix computation as a helper method. It seems to be “the trick” that comes up over and over again to make things parallel. Section 5.2 gives an example, developing an algorithm on top of parallel-prefix sum. We will then use _that_ algorithm to implement a parallel variant of quicksort.
