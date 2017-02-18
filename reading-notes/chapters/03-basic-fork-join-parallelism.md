## 3 Basic Fork-Join Parallelism

This section shows how to use threads and shared memory to implement simple parallel algorithms. The only synchronization primitive we will need is `join`,  which causes one thread to wait until another thread has terminated. We begin with simple pseudocode and then show how using threads in C# to achieve the same idea requires a bit more work (Section 3.1). We then argue that it is best for parallel code to _not_ be written in terms of the number of processors available (Section 3.2) and show how to use recursive divide-and-conquer instead (Section 3.3). Because C#’s threads are not engineered for this style of programming, we switch to the C# Task Parallel library which is designed for our needs (Section 3.4).  With all of this discussion in terms of the single problem of summing an array of integers, we then turn to other similar problems, introducing the terminology of _maps_ and _reduces_ (Section 3.5) as well as data structures other than arrays (Section 3.6).

### 3.1 A Simple Example: Okay Idea, Inferior Style

Most of this section will consider the problem of computing the sum of an array of integers. An _O(n)_ sequential solution to this problem is trivial:

    int Sum(int[] arr)
    {
        int ans = 0;
        for (int i = 0; i < arr.Length; i++)
            ans += arr[i];
        return ans;
    }

If the array is large and we have extra processors available, we can get a more efficient parallel algorithm. Suppose we have 4 processors. Then we could do the following:

 * Use the first processor to sum the first 1/4 of the array and store the result somewhere.
 * Use the second processor to sum the second 1/4 of the array and store the result somewhere.
 * Use the third processor to sum the third 1/4 of the array and store the result somewhere.
 * Use the fourth processor to sum the fourth 1/4 of the array and store the result somewhere.
 * Add the 4 stored results and return that as the answer.

This algorithm is clearly correct provided that the last step is started only after the previous four steps have completed. The first four steps can occur in parallel. More generally, if we have _P_ processors, we can divide the array into _P_ equal segments and have an algorithm that runs in time _O(n/P + P)_ where _n/P_ is for the parallel part and _P_ is for combining the stored results. Later we will see we can do better if _P_ is very large, though that may be less of a practical concern.

_In pseudocode_, a convenient way to write this kind of algorithm is with a `FORALL` loop. A `FORALL` loop is like a `for` loop except it does all the iterations in parallel. Like a regular `for` loop, the code after a `FORALL` loop does not execute until the loop (i.e., all its iterations) are done. Unlike the `for` loop, the programmer is “promising” that all the iterations can be done at the same time without them interfering with each other. Therefore, if one loop iteration writes to a location, then another iteration must not read or write to that location. However, it is fine for two iterations to read the same location: that does not cause any interference.

Here, then, is a pseudocode solution to using 4 processors to sum an array. Note it is essential that we store the 4 partial results in separate locations to avoid any interference between loop iterations (Note: We must take care to avoid bugs due to integer-division truncation with the arguments to `sumRange`. We need to process each array element exactly once even if `len` is not divisible by 4. This code is correct; notice in particular that `((i+1)*len)/4` will always be `len` when `i==3` because `4*len` is divisible by 4. Moreover, we could write `(i+1)*len/4` since `*` and `/` have the same precedence and associate left-to-right. But `(i+1)*(len/4)` would _not_ be correct. For the same reason, defining a variable `int rangeSize = len/4` and using `(i+1)*rangeSize` would _not_ be correct.).

    int Sum(int[] arr) {
        results = new int[4];
        len = arr.Length;
        FORALL(i=0; i < 4; ++i) {
            results[i] = sumRange(arr,(i*len)/4,((i+1)*len)/4);
        }
        return results[0] + results[1] + results[2] + results[3];
    }
    int SumRange(int[] arr, int lo, int hi) {
        result = 0;
        for(j=lo; j < hi; ++j)
            result += arr[j];
        return result;
    }

Unfortunately, C# and most other general-purpose languages do not have a `FORALL` loop. (C# has various kinds of for-loops, but all run all iterations on one thread.) We can encode this programming pattern explicitly using threads as follows:

 1. In a regular `for` loop, create one thread to do each iteration of our `FORALL` loop, passing the data needed in the constructor. Have the threads store their answers in fields of themselves.
 2. Wait for all the threads created in step 1 to terminate.
 3. Combine the results by reading the answers out of the fields of the threads created in step 1.

To understand this pattern, we will first show a _wrong_ version to get the idea. That is a common technique in these notes — learning from wrong versions is extremely useful — but wrong versions are always clearly indicated.

Here is our WRONG attempt:

    class SumRange
    {
        int left;
        int right;
        int[] arr;
        public int Answer { get; private set; }

        public SumRange(int[] a, int left, int right)
        {
            this.left = left;
            this.right = right;
            this.arr = a;
            Answer = 0;
        }

        public void Run()
        {
            for (int i = left; i < right; i++)
            {
                Answer += arr[i];
            }
        }
    }

    public static int Sum(int[] arr)
    {
        int len = arr.Length;
        int ans = 0;

        SumRange[] s = new SumRange[4];
        for (int i = 0; i < 4; i++)
        {
            SumRange sr = new SumRange(arr, (i * len) / 4, ((i + 1) * len) / 4);
            s[i] = sr;
            Thread t = new Thread(() => sr.Run());
            t.Start();
        }

        for (int i = 0; i < 4; i++)
        {
            ans += s[i].Answer;
        }

        return ans;
    }

The code above gets most of the pieces right. The `Sum` method creates 4 instances of `SumRange` and then creates instances of `System.Threading.Thread` to call `Run` on each of them. The `SumRange` constructor takes as arguments the data that the thread needs to do its job, in this case, the array and the range for which this thread is responsible. (We use a convenient convention that ranges _include_ the low bound and _exclude_ the high bound). The `SumRange` constructor stores this data in fields of the object so that it has access to them in the `Run` method.

Notice each `SumRange` object also has an `Answer` property. This is shared memory for communicating the answer back from the helper thread to the main thread. So the main thread can sum the 4 `Answer` properties from the threads it created to produce the final answer.

The bug in this code has to do with synchronization: the `main` thread does not wait for the helper threads to finish before it sums the `Answer` properties. Remember that `Start` returns immediately — otherwise we would not get any parallelism. So the `Sum` method’s second for-loop probably starts running before the helper threads are finished with their work. Having one thread (the main thread) read a field while another thread (the helper thread) is writing the same field is a bug, and here it would produce a wrong (too-small) answer. We need to delay the second for-loop until the helper threads are done.

There is a method in `System.Threading.Thread` that is just what we need. If one thread, in our case the main thread, calls the `Join` method of a `System.Threading.Thread` object, in our case one of the helper threads, then this call _blocks_ (i.e., does not return) unless/until the thread corresponding to the object has terminated. So we can add another for-loop to
`Sum` in-between the two loops already there to make sure all the helper threads finish before we add together the results:

    for(int i=0; i < 4; i++)
        t[i].Join();

Notice it is the main thread that is calling `Join`, which takes no arguments. On the first loop iteration, the main thread will block until the first helper thread is done. On the second loop iteration, the main thread will block until the second helper thread is done. It is certainly possible that the second helper thread actually finished before the first thread. This is not a problem: a call to `Join` when the helper thread has already terminated just returns right away (no blocking).

Essentially, we are using two for-loops, where the first one creates helper threads and the second one waits for them all to terminate, to encode the idea of a FORALL-loop. This style of parallel programming is called “fork-join parallelism.” It is like we create a “(4-way in this case) fork in the road of execution” and send each helper thread down one path of the fork. Then we join all the paths of the fork back together and have the single main thread continue. Fork-join parallelism can also be _nested_ , meaning one of the helper threads forks its own helper threads. In fact, we will soon argue that this is better style. The term “join” is common in different programming languages and libraries, though honestly it is not the most descriptive English word for the concept.

It is common to combine the joining for-loop and the result-combining for-loop. Understanding why this is still correct helps understand the `Join` primitive. So far we have suggested writing code like this in our `Sum` method:

    for(int i=0; i < 4; i++)
        t[i].Join();
    for(int i=0; i < 4; i++)
        ans += s[i].Answer;
    return ans;

There is nothing wrong with the code above, but the following is also correct:

    for(int i=0; i < 4; i++) {
        t[i].Join();
        ans += s[i].Answer;
    }
    return ans;

Here we do not wait for all the helper threads to finish before we start producing the final answer. But we still ensure that the main thread does not access a helper thread’s `Answer` property until at least that helper thread has terminated.

Here, then, is a complete and correct program. There is no change to the `SumRange` class. This example shows many of the key concepts of fork-join parallelism, but Section 3.2 will explain why it is poor style and can lead to suboptimal performance. Sections 3.3 and 3.4 will then present a similar but better approach.

    class SumRange
    {
        int left;
        int right;
        int[] arr;
        public int Answer { get; private set; }

        public SumRange(int[] a, int left, int right)
        {
            this.left = left;
            this.right = right;
            this.arr = a;
            Answer = 0;
        }

        public void Run()
        {
            for (int i = left; i < right; i++)
            {
                Answer += arr[i];
            }
        }
    }

    public static int Sum(int[] arr)
    {
        int len = arr.Length;
        int ans = 0;

        SumRange[] s = new SumRange[4];
        Thread[] t = new Thread[4];
        for (int i = 0; i < 4; i++)
        {
            SumRange sr = new SumRange(arr, (i * len) / 4, ((i + 1) * len) / 4);
            s[i] = sr;
            t[i] = new Thread(sr.Run);
            t[i].Start();
        }

        for (int i = 0; i < 4; i++)
        {
            t[i].Join();
            ans += s[i].Answer;
        }

        return ans;
    }

### 3.2 Why Not To Use One Thread Per Processor

Having now presented a basic parallel algorithm, we will argue that the approach the algorithm takes is poor style and likely to lead to unnecessary inefficiency. Do not despair: the concepts we have learned like creating threads and using `Join` will remain useful — and it was best to explain them using a too-simple approach. Moreover, many parallel programs are written in pretty much exactly this style, often because libraries like those in Section 3.4 are unavailable. Fortunately, such libraries are now available on many platforms.

The problem with the previous approach was dividing the work into exactly 4 pieces. This approach assumes there are 4 processors available to do the work (no other code needs them) and that each processor is given approximately the same amount of work. Sometimes these assumptions may hold, but it would be better to use algorithms that do not rely on such brittle assumptions. The rest of this section explains in more detail why these assumptions are unlikely to hold and some partial solutions. Section 3.3 then describes the better solution that we advocate.

__Different computers have different numbers of processors__

We want parallel programs that effectively use the processors available to them. Using exactly 4 threads is a horrible approach. If 8 processors are available, half of them will sit idle and our program will be no faster than with 4 processors. If 3 processors are available, our 4-thread program will take approximately twice as long as with 4 processors. If 3 processors are available and we rewrite our program to use 3 threads, then we will use resources effectively and the result will only be about 33% slower than when we had 4 processors and 4 threads. (We will take 1/3 as much time as the sequential version compared to 1/4 as much time. And 1/3 is 33% slower than 1/4.) But we do not want to have to edit our code every time we run it on a computer with a different number of processors.

A natural solution is a core software-engineering principle you should already know: do not use constants where a variable is appropriate. Our `Sum` method can take as a parameter the number of threads to use, leaving it to some other part of the program to decide the number. (There are C# library methods to ask for the number of processors on the computer, for example, but we argue next that using that number is often unwise.) It would look like this:

    public static int Sum(int[] arr, int numThreads)
    {
        int len = arr.Length;
        int ans = 0;

        SumRange[] s = new SumRange[numThreads];
        Thread[] t = new Thread[numThreads];
        for (int i = 0; i < numThreads; i++)
        {
            SumRange sr = new SumRange(arr, (i * len) / numThreads, ((i + 1) * len) / numThreads);
            s[i] = sr;
            t[i] = new Thread(sr.Run);
            t[i].Start();
        }

        for (int i = 0; i < numThreads; i++)
        {
            t[i].Join();
            ans += s[i].Answer;
        }

        return ans;
    }

Note that you need to be careful with integer division not to introduce rounding errors when dividing the work.

__The processors available to part of the code can change__

The second dubious assumption made so far is that every processor is available to the code we are writing. But some processors may be needed by other programs or even other parts of the same program. We have parallelism after all — maybe the caller to `Sum` is already part of some outer parallel algorithm. The operating system can reassign processors at any time, even when we are in the middle of summing array elements. It is fine to assume that the underlying C# implementation will try to use the available processors effectively, but we should not assume 4 or even `numThreads` processors will be available from the beginning to the end of running our parallel algorithm.

__We cannot always predictably divide the work into approximately equal pieces__

In our `Sum` example, it is quite likely that the threads processing equal-size chunks of the array take approximately the same amount of time.   They may not, due to memory-hierarchy issues or other architectural effects,  however. Moreover, more sophisticated algorithms could produce a large _load imbalance_, meaning different helper threads are given different amounts of work. As a simple example (perhaps too simple for it to actually matter), suppose we have a large `int[]` and we want to know how many elements of the array are prime numbers. If one portion of the array has more large prime numbers than another, then one helper thread may take longer.

In short, giving each helper thread an equal number of data elements is not necessarily the same as giving each helper thread an equal amount of work. And any load imbalance hurts our efficiency since we need to wait until all threads are completed.

__A solution: Divide the work into smaller pieces__

We outlined three problems above. It turns out we can solve all three with a perhaps counterintuitive strategy: _Use substantially more threads than there are processors._ For example, suppose to sum the elements of an array we created one thread for each 1000 elements. Assuming a large enough array (size greater than 1000 times the number of processors), the threads will not all run at once since a processor can run at most one thread at a time. But this is fine: the system will keep track of what threads are waiting and keep all the processors busy. There is some overhead to creating more threads, so we should use a system where this overhead is small.

This approach clearly fixes the first problem: any number of processors will stay busy until the very end when there are fewer 1000-element chunks remaining than there are processors. It also fixes the second problem since we just have a “big pile” of threads waiting to run. If the number of processors available changes, that affects only how fast the pile is processed, but we are always doing useful work with the resources available. Lastly, this approach helps with the load imbalance problem: smaller chunks of work make load imbalance far less likely since the threads do not run as long. Also, if one processor has a slow chunk, other processors can continue processing faster chunks.

We can go back to our cutting-potatoes analogy to understand this approach: rather than give each of 4 cooks (processors) 1/4 of the potatoes, we have them each take a moderate number of potatoes, slice them, and then return to take another moderate number. Since some potatoes may take longer than others (they might be dirtier or have more eyes), this approach is better balanced and is probably worth the cost of the few extra trips to the pile of potatoes — especially if one of the cooks might take a break (processor used for a different program) before finishing his/her pile.

Unfortunately, this approach still has two problems addressed in Sections 3.3 and 3.4:

 1. We now have more results to combine.  Dividing the array into 4 total pieces leaves _Θ(1)_ results to combine. Dividing  the  array  into  1000-element  chunks  leaves `arr.Length/1000`,  which  is _Θ(n)_, results  to  combine.   Combining the results with a sequential for-loop produces an _Θ(n)_ algorithm, albeit with a smaller constant factor. To see the problem even more clearly, suppose we go to the extreme and use 1-element chunks — now the results combining reimplements the original sequential algorithm. In short, we need a better way to combine results.
 2. C#'s threads were not designed for small tasks like adding 1000 numbers.  They will work and produce the correct answer, but the constant-factor overheads of creating a C# thread are far too large. A C# program that creates 100,000 threads on a small desktop computer is unlikely to run well at all — each thread just takes too much memory and the scheduler is overburdened and provides no asymptotic run-time guarantee. In short, we need a different implementation of threads that is designed for this kind of fork/join programming.


### 3.3 Divide-And-Conquer Parallelism

This section presents the idea of divide-and-conquer parallelism using C# threads. Then Section 3.4 switches to using a library where this programming style is actually efficient. This progression shows that we can understand all the ideas using the basic notion of threads even though in practice we need a library that is designed for this kind of programming.

The key idea is to _change our algorithm_ for summing the elements of an array to use recursive divide-and-conquer. To sum all the array elements in some range from `lo` to `hi`, do the following:

 1. If the range contains only one element, return that element as the sum. Else in parallel:
  1.1. Recursively sum the elements from `lo` to the middle of the range.
  1.2. Recursively sum the elements from the middle of the range to `hi`.
 2. Add the two results from the previous step.

The essence of the recursion is that steps 1a and 1b will themselves use parallelism to divide the work of their halves in half again. It is the same divide-and-conquer recursive idea as you have seen in algorithms like mergesort. For sequential algorithms for simple problems like summing an array, such fanciness is overkill. But for parallel algorithms, it is ideal.

As a small example (too small to actually want to use parallelism), consider summing an array with 10 elements. The algorithm produces the following tree of recursion, where the range `[i,j)` includes `i` and excludes `j`:

    Thread: sum range [0,10)
        Thread: sum range [0,5)
            Thread: sum range [0,2)
                Thread: sum range [0,1) (return arr[0])
                Thread: sum range [1,2) (return arr[1])
                add results from two helper threads
            Thread: sum range [2,5)
                Thread: sum range [2,3) (return arr[2])
                Thread: sum range [3,5)
                    Thread: sum range [3,4) (return arr[3])
                    Thread: sum range [4,5) (return arr[4])
                    add results from two helper threads
                add results from two helper threads
            add results from two helper threads
        Thread: sum range [5,10)
            Thread: sum range [5,7)
                Thread: sum range [5,6) (return arr[5])
                Thread: sum range [6,7) (return arr[6])
                add results from two helper threads
            Thread: sum range [7,10)
                Thread: sum range [7,8) (return arr[7])
                Thread: sum range [8,10)
                    Thread: sum range [8,9) (return arr[8])
                    Thread: sum range [9,10) (return arr[9])
                    add results from two helper threads
                add results from two helper threads
            add results from two helper threads
        add results from two helper threads

The total amount of work done by this algorithm is _O(n)_ because we create approximately _2n_ threads and each thread either returns an array element or adds together results from two helper threads it created. Much more interestingly, if we have _O(n)_ processors, then this algorithm can run in _O(log n)_ time, which is exponentially faster than the sequential algorithm. The key reason for the improvement is that the algorithm is combining results in parallel. The recursion forms a binary tree for summing subranges and the height of this tree is _log n_ for a range of size _n_. See Figure 5, which shows the recursion in a more conventional tree form where the number of nodes is growing exponentially faster than the tree height. With enough processors, the total running time corresponds to the tree _height_, not the tree _size_:  this is the fundamental running-time benefit of parallelism. Later sections will discuss why the problem of summing an array has such an efficient  parallel algorithm; not every problem enjoys exponential improvement from parallelism.

Having described the algorithm in English, seen an example, and informally analyzed its running time, let us now consider an actual implementation with C# threads and then modify it with two important improvements that affect only constant factors, but the constant factors are large. Then the next section will show the “final” version where we use the improvements and use a different library for the threads.

To start, here is the algorithm directly translated into C#, omitting some boilerplate like putting the main `Sum` method in a class and handling exceptions (Note - This may fail to compute the correct result in default .Net settings. By default every thread in 32 bit app gets 1 Mb of memory in .Net. And the whole app gets 2 Gb. So creating about 2000 threads can easily make app run out of memory).

    class SumRange
    {
        int left;
        int right;
        int[] arr;
        public int Answer { get; private set; }

        public SumRange(int[] a, int l, int r)
        {
            left = l;
            right = r;
            arr = a;
            Answer = 0;
        }

        public void Run()
        {
            if (right - left == 1)
            {
                Answer = arr[left];
            }
            else
            {
                SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                Thread leftThread = new Thread(leftRange.Run);
                Thread rightThread = new Thread(rightRange.Run);
                leftThread.Start();
                rightThread.Start();
                leftThread.Join();
                rightThread.Join();

                Answer = leftRange.Answer + rightRange.Answer;
            }
        }
    }

    public static int Sum(int[] arr)
    {
        SumRange s = new SumRange(arr, 0, arr.Length);
        s.Run();
        return s.Answer;
    }

Notice how each thread creates two helper threads `leftThread` and `rightThread` and then waits for them to finish. Crucially, the calls to `leftThread.Start` and `rightThread.start` precede the calls to `leftThread.Join` and `right.ThreadJoin`. If for example, `leftThread.Join()` came before `rightThread.Start()`, then the algorithm would have no effective  parallelism whatsoever. It would still produce the correct answer, but so would the original much simpler sequential program.

In practice, code like this produces far too many threads to be efficient. To add up four numbers, does it really make sense to create six new threads? Therefore, implementations of fork/join algorithms invariably use a _cutoff_ below which they switch over to a sequential algorithm. Because this cutoff is a constant, it has no effect on the asymptotic behavior of the algorithm. What it does is eliminate the vast majority of the threads created, while still preserving enough parallelism to balance the load among the processors.

Here is code using a cutoff of 1000. As you can see, using a cutoff does not really complicate the code.

    class SumRange
    {
        static int Sequential_Cutoff = 100;
        int left;
        int right;
        int[] arr;
        public int Answer { get; private set; }

        public SumRange(int[] a, int l, int r)
        {
            left = l;
            right = r;
            arr = a;
            Answer = 0;
        }

        public void Run()
        {
            if (right - left < Sequential_Cutoff)
            {
                for (int i = left; i < right; i++)
                {
                    Answer += arr[i];
                }
            }
            else
            {
                SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                Thread leftThread = new Thread(leftRange.Run);
                Thread rightThread = new Thread(rightRange.Run);
                leftThread.Start();
                rightThread.Start();
                leftThread.Join();
                rightThread.Join();

                Answer = leftRange.Answer + rightRange.Answer;
            }
        }
    }

    public static int Sum(int[] arr)
    {
        SumRange s = new SumRange(arr, 0, arr.Length);
        s.Run();
        return s.Answer;
    }

Using cut-offs is common in divide-and-conquer programming, even for sequential algorithms. For example, it is typical for quicksort to be slower than an _O(n<sup>2</sup>) sort like insertionsort for small arrays (_n<10_ or so). Therefore, it is common to have the recursive quicksort switch over to insertionsort for small subproblems. In parallel programming, switching over to a sequential algorithm below a cutoff is _the exact same idea_. In practice, the cutoffs are usually larger, with numbers between 500 and 5000 being typical.

It is often worth doing some quick calculations to understand the benefits of things like cutoffs. Suppose we are summing an array with 2<sup>30</sup> elements. Without a cutoff, we would use 2<sup>31</sup> − 1, (i.e., two billion) threads. With a cutoff of 1000, we would use approximately 2<sup>21</sup> (i.e., 2 million) threads since the last 10 levels of the recursion would be eliminated. Computing 1 − 2<sup>21</sup>/2<sup>31</sup>, we see we have eliminated 99.9% of the threads. Use cutoffs!

Our second improvement may  seem anticlimactic compared to cutoffs because it only reduces the number of threads by an additional factor of two. Nonetheless, it is worth seeing for efficiency especially because the Task Parallel in the next section performs poorly if you do not do this optimization “by hand”. The key is to notice that all threads that create two helper threads are not doing much work themselves: they divide the work in half, give it to two helpers, wait for them to finish, and add the results.  Rather than having all these threads wait around, it is more efficient to create _one helper thread_ to do half the work and have the thread do the other half _itself_. Modifying our code to do this is easy since we can just call the `Run` method directly, without passing it into "magic" `Thread` object.

    class SumRange
    {
        static int Sequential_Cutoff = 100;
        int left;
        int right;
        int[] arr;
        public int Answer { get; private set; }

        public SumRange(int[] a, int l, int r)
        {
            left = l;
            right = r;
            arr = a;
            Answer = 0;
        }

        public void Run()
        {
            if (right - left < Sequential_Cutoff)
            {
                for (int i = left; i < right; i++)
                {
                    Answer += arr[i];
                }
            }
            else
            {
                SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                Thread leftThread = new Thread(leftRange.Run);
                leftThread.Start();
                rightRange.Run();
                leftThread.Join();

                Answer = leftRange.Answer + rightRange.Answer;
            }
        }
    }

    public static int Sum(int[] arr)
    {
        SumRange s = new SumRange(arr, 0, arr.Length);
        s.Run();
        return s.Answer;
    }

Notice how the code above creates two `SumRange` objects, but only creates one helper thread. It then does the right half of the work itself by calling `rightRange.Run()`. There is only one call to `Join` because only one helper thread was created. The order here is still essential so that the two halves of the work are done in parallel. Creating a `SumRange` object for the right half and then calling `Run` rather than creating a thread may seem odd, but
it keeps the code from getting more complicated and still conveys the idea of dividing the work into two similar parts that are done in parallel.

Unfortunately, even with these optimizations, the code above will run poorly in practice, especially if given a large array. The implementation of C# threads is not engineered for threads that do such a small amount of work as adding 1000 numbers: it takes much longer just to create, start running, and dispose of a thread. The space overhead may also be prohibitive. In particular, it is not uncommon for a C# implementation to pre-allocate some amount of memory for the stack, which might be 1MB or more. So creating thousands of threads could use gigabytes of space. Hence we will switch to the library described in the next section for parallel programming. We will return to C# threads when we learn concurrency because the synchronization operations we will use work with C# threads.

### 3.4 The C# Task Parallel Library

.Net 4 (and higher) includes classes in `System.Threading` and `System.Threading.Tasks` namespaces designed exactly for the kind of fine-grained fork-join parallel computing these notes use. Collection of these classes is called Task Parallel Library or TPL. In addition to supporting lightweight threads (which the library calls Tasks) that are small enough that even a million of them should not overwhelm the system, the implementation includes a scheduler and run-time system with provably optimal expected-time guarantees. Similar libraries for other languages include Intel’s Thread Building Blocks, Java ForkJoin Framework, and others. The core ideas and implementation techniques go back much further to the Cilk language, an extension of C developed since 1994.

This section describes just a few practical details and library specifics. Compared to C# threads, the core ideas are all the same, but some of the method names and interfaces are different — in places more complicated and in others simpler. Naturally, we give a full example (actually two) for summing an array of numbers. The actual library contains many other useful features and classes, but we will use only the primitives related to forking and joining, implementing anything else we need ourselves.

We first show a full program that is as much as possible like the version we wrote using C# threads. We show a version using a sequential cut-off and only one helper thread at each recursive subdivision though removing these important improvements would be easy. After discussing this version, we show  a second version that uses C# generic types and a different library class. This second version is better style, but easier to understand after the first version.

FIRST VERSION (INFERIOR STYLE):

    using System.Threading.Tasks;

    public class  public class DivideAndConquerTaskParallel
    {
        class SumRange
        {
            static int Sequential_Cutoff = 100;
            int left;
            int right;
            int[] arr;
            public int Answer { get; private set; }

            public SumRange(int[] a, int l, int r)
            {
                left = l;
                right = r;
                arr = a;
                Answer = 0;
            }

            public void Run()
            {
                if (right - left < Sequential_Cutoff)
                {
                    for (int i = left; i < right; i++)
                    {
                        Answer += arr[i];
                    }
                }
                else
                {
                    SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                    SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                    Task leftTask = Task.Factory.StartNew(leftRange.Run);
                    rightRange.Run();
                    leftTask.Wait();

                    Answer = leftRange.Answer + rightRange.Answer;
                }
            }
        }

        public static int Sum(int[] arr)
        {
            SumRange s = new SumRange(arr, 0, arr.Length);
            s.Run();
            return s.Answer;
        }
    }

There are some differences compared to using C# threads, but the overall structure of the algorithm should look similar. Furthermore, most of the changes are just different names for classes and methods:

 * Class `System.Threading.Tasks.Task` instead of `System.Threading.Thread`.
 * Parallelism starts with static method call `Task.Factory.StartNew`.
 * Method for waiting another task's finish is now called `Wait` instead of `Join`.

Such details as starting a new task with `System.Threading.Thread` are there because the library is not built into the C# _language_, so we have to do a little extra to use it. What you really need to know is that `Task` instances should not be created explicitly, but rather by the tasks factory, so that the library could deal with underlying details such as the partitioning of the work, the scheduling of threads on the `ThreadPool`, cancellation support, state management, and other low-level details. Otherwise `Task` is very similar to `Thread`, with `Start` and `Wait` being used just as we used `Start` and `Join` before.

We will present one final version of our array-summing program to demonstrate one more aspect of TPL that you should use as a matter of style. The `Task` class is best only when the subcomputations do not produce a result, whereas in our example they do: the sum of the range. It is quite common not to produce a result, for example a parallel program that increments every element of an array. So far, the way we have “returned” results is via a property, which we called `Answer`.

Instead, we can use generic class `Task<T>` instead of `Task`. The type parameter here is the type of value that passed delegate should return. Here is the full version of the code using this more convenient and less error-prone class, followed by an explanation:

FINAL, BETTER VERSION:

    using System.Threading.Tasks;

    public class DivideAndConquerTaskParallelResult
    {
        class SumRange
        {
            static int Sequential_Cutoff = 100;
            int left;
            int right;
            int[] arr;

            public SumRange(int[] a, int l, int r)
            {
                left = l;
                right = r;
                arr = a;
            }

            public int Run()
            {
                if (right - left < Sequential_Cutoff)
                {
                    int ans = 0;
                    for (int i = left; i < right; i++)
                    {
                        ans += i;
                    }
                    return ans;
                }
                else
                {
                    SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                    SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                    Task<int> leftTask = Task.Factory.StartNew<int>(leftRange.Run);
                    int rightAns = rightRange.Run();
                    leftTask.Wait();
                    int leftAns = leftTask.Result;

                    return leftAns + rightAns;
                }
            }
        }

        public static int Sum(int[] arr)
        {
            SumRange s = new SumRange(arr, 0, arr.Length);
            return s.Run();
        }
    }

Here are the differences from the version that uses non-generic `Task`:

 * `Answer` property is gone.
 * `Run` returns an integer as a result of computation.
 * We use `Task<int>` instead of `Task`.
 * Tasks now have an additional property `Result` which we use to get result of a task run.

If you are familiar with C# generic types, this use of them should not be particularly perplexing. The library is also using static overloading for the `StartNew` method. But as _users_ of the library, it suffices just to know that you can follow the same pattern no matter which type of task you are using.

### 3.5 Reductions and Maps

It may seem that given all the work we did to implement something as conceptually simple as summing an array that fork/join programming is too complicated. To the contrary, it turns out that many, many problems can be solved very much like we solved this one. Just like regular for-loops took some getting used to when you started programming but now you can recognize exactly what kind of loop you need for all sorts of problems, divide-and-conquer parallelism often follows a small number of patterns. Once you know the patterns, most of your programs are largely the same.

For example, here are several problems for which efficient parallel algorithms look almost identical to summing an array:

 * Count how many array elements satisfy some property (e.g., how many elements are the number 42).
 * Find the maximum or minimum element of an array.
 * Given an array of strings, compute the sum (or max, or min) of all their lengths.
 * Find the left-most array index that has an element satisfying some property.

Compared to summing an array, all that changes is the base case for the recursion and how we combine results. For example, to find the index of the leftmost 42 in an array of length _n_, we can do the following (where a final result of _n_ means the array does not hold a 42):

 * For the base case, return `lo` if `arr[lo]` holds 42 and `n` otherwise.
 * To combine results, return the smaller number.

Implement one or two of these problems to convince yourself they are not any harder than what we have already done. Or come up with additional problems that can be solved the same way.

Problems that have this form are so common that there is a name for them: _reductions_, which you can remember by realizing that we take a collection of data items (in an array) and _reduce_ the information down to a single result. As we have seen, the way reductions can work in parallel is to compute answers for the two halves recursively and in parallel and then merge these to produce a result.

However, we should be clear that _not every problem over an array of data can be solved with a simple parallel reduction_. To avoid getting into arcane problems, let’s just describe a general situation. Suppose you have sequential code like this:

    interface BinaryOperation<T>
    {
        T M(T x, T y);
    }

    class C<T>
    {
        T Fold(T[] arr, BinaryOperation<T> binop, T initialValue)
        {
            T ans = initialValue;
            for (int i = 0; i < arr.Length; i++)
                ans = binop.M(ans, arr[i]);
            return ans;
        }
    }

The name `Fold` is conventional for this sort of algorithm. The idea is to start with `initialValue` and keep updating the “answer so far” by applying some binary function `M` to the current answer and the next element of the array.

Without any additional information about what `M` computes, this algorithm cannot be effectively parallelized since we cannot process `arr[i]` until we know the answer from the first `i-1` iterations of the for-loop. For a more humorous example of a procedure that cannot be sped up given additional resources: 9 women can’t make a baby in 1 month.

So what do we have to know about the `BinaryOperation` above in order to use a parallel reduction? It turns out all we need is that the operation is _associative_ _, meaning for all _a_, _b_, and _c_, _M(a, M(b, c))_ is the same as _M(M(a, b), c)_. Our array-summing algorithm is correct because _a + (b + c) = (a + b) + c_.  Our find-the-leftmost-index-holding 42 algorithm is correct because _min_ is also an associative operator.

Because reductions using associative operators are so common, we could write one generic algorithm that took the operator, and what to do for a base case, as arguments. This is an example of higher-order programming, and the Task Parallel Library has several classes providing this sort of functionality. Higher-order programming has many, many advantages (see the end of this section for a popular one), but when first _learning_ a programming pattern, it is often useful to “code it up yourself” a few times. For that reason, we encourage writing your parallel reductions manually in order to see the parallel divide-and-conquer, even though they all really look the same.

Parallel reductions are not the only common pattern in parallel programming. An even simpler one, which we did not start with because it is just so easy, is a parallel _map_. A map performs an operation on each input element independently; given an array of inputs, it produces an array of outputs of the same length. A simple example would be multiplying every element of an array by 2. An example using two inputs and producing a separate output would be vector addition. Using pseudocode, we could write:

    int[] add(int[] arr1, int[] arr2)
    {
        assert(arr1.length == arr2.length);
        int[] ans = new int[arr1.length];
        FORALL(int i = 0; i < arr1.length; i++)
            ans[i] = arr1[i] + arr2[i];
        return ans;
    }

Coding up this algorithm in the Task Parallel Library is straightforward:  Have the main thread create the `ans` array and pass it before starting the parallel divide-and-conquer. Each thread object will have a reference to this array but will assign to different portions of it. Because there are no other results to combine, using `Task` is appropriate. Using a sequential cut-off and creating only one new thread for each recursive subdivision of the problem remain important — these ideas are more general than the particular programming pattern of a map or a reduce.

Recognizing problems that are fundamentally maps and/or reduces over large data collections is a valuable skill that allows efficient parallelization. In fact, it is one of the key ideas behind Google’s MapReduce framework and the open-source variant Hadoop. In these systems, the programmer just writes the operations that describe how to map data (e.g., “multiply by 2”) and reduce data (e.g., “take the minimum”). The system then does all the parallelization, often using hundreds or thousands of computers to process gigabytes or terabytes of data. For this to work, the programmer must provide operations that have no side effects (since the order they occur is unspecified) and reduce operations that are associative (as we discussed). As parallel programmers, it is often enough to “write down the maps and reduces” — leaving it to systems like the TPL or Hadoop to do the actual scheduling of the parallelism.

### 3.6 Data Structures Besides Arrays

So far we have considered only algorithms over one-dimensional arrays.  Naturally, one can write parallel algorithms over any data structure, but divide-and-conquer parallelism requires that we can efficiently (ideally in _O(1)_ time) divide the problem into smaller pieces. For arrays, dividing the problem involves only _O(1)_ arithmetic on indices, so this works well.

While arrays are the most common data structure in parallel programming, balanced trees, such as AVL trees or B trees, also support parallel algorithms well. For example, with a binary tree, we can fork to process the left child and right child of each node in parallel. For good sequential cut-offs, it helps to have stored at each tree node the number of descendants of the node, something easy to maintain. However, for trees with guaranteed balance properties, other information — like the height of an AVL tree node — should suffice.

Certain tree problems will not run faster with parallelism. For example, searching for an element in a balanced binary search tree takes _O(log n)_ time with or without parallelism. However, maps and reduces over balanced treesbenefit from parallelism. For example, summing the elements of a binary tree takes _O(n)_ time sequentially where _n_ is the number of elements, but with a sufficiently large number of processors, the time is _O(h)_, where _h_ is the height of the tree. Hence, tree balance is even more important with parallel programming: for a balanced tree h = _Θ(log n)_ compared to the worst case _h = Θ(n)_.

For the same reason, parallel algorithms over regular linked lists are typically poor. Any problem that requires reading all _n_ elements of a linked list takes time _Ω(n)_ regardless of how many processors are available. (Fancier list data structures like skip lists are better for exactly this reason — you can get to all the data in _O(log n)_time.) Streams of input data, such as from files, typically have the same limitation: it takes linear time to read the input and this can be the bottleneck for the algorithm.

There can still be benefit to parallelism with such “inherently sequential” data structures and input streams. Suppose we had a map operation over a list but each operation was itself an expensive computation (e.g., decrypting a significant piece of data). If each map operation took time _O(x)_ and the list had length _n_, doing each operation in a separate thread (assuming,  again, no limit on the number of processors) would produce an _O(x + n)_ algorithm compared to the sequential _O(xn)_ algorithm. But for simple operations like summing or finding a maximum element, there would be no benefit.