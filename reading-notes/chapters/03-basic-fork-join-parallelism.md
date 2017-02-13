## 3 Basic Fork-Join Parallelism

This section shows how to use threads and shared memory to implement simple parallel algorithms. The only synchronization primitive we will need is `join`,  which causes one thread to wait until another thread has terminated. We begin with simple pseudocode and then show how using threads in C# to achieve the same idea requires a bit more work (Section 3.1). We then argue that it is best for parallel code to _not_ be written in terms of the number of processors available (Section 3.2) and show how to use recursive divide-and-conquer instead (Section 3.3). Because C#’s threads are not engineered for this style of programming, we switch to the C# Task Parallel library which is designed for our needs (Section 3.4).  With all of this discussion in terms of the single problem of summing an array of integers, we then turn to other similar problems, introducing the terminology of _maps_ and _reduces_ (Section 3.5) as well as data structures other than arrays (Section 3.6).

### 3.1 A Simple Example: Okay Idea, Inferior Style

Most of this section will consider the problem of computing the sum of an array of integers. An _O(n)__ sequential solution to this problem is trivial:

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

### 3.2  Why Not To Use One Thread Per Processor

Having now presented a basic parallel algorithm, we will argue that the approach the algorithm takes is poor style and likely to lead to unnecessary inefficiency. Do not despair: the concepts we have learned like creating threads and using `Join` will remain useful — and it was best to explain them using a too-simple approach. Moreover, many parallel programs are written in pretty much exactly this style, often because libraries like those in Section 3.4 are unavailable. Fortunately, such libraries are now available on many platforms.

The problem with the previous approach was dividing the work into exactly 4 pieces. This approach assumes there are 4 processors available to do the work (no other code needs them) and that each processor is given approximately the same amount of work. Sometimes these assumptions may hold, but it would be better to use algorithms that do not rely on such brittle assumptions. The rest of this section explains in more detail why these assumptions are unlikely to hold and some partial solutions. Section 3.3 then describes the better solution that we advocate.

__Different computers have different numbers of processors__

We want parallel programs that effectively use the processors available to them. Using exactly 4 threads is a horrible approach. If 8 processors are available, half of them will sit idle and our program will be no faster than
with 4 processors. If 3 processors are available, our 4-thread program will take approximately twice as long as with 4 processors. If 3 processors are available and we rewrite our program to use 3 threads, then we will use resources effectively and the result will only be about 33% slower than when we had 4 processors and 4 threads. (We will take 1/3 as much time as the sequential version compared to 1/4 as much time. And 1/3 is 33% slower than 1/4.) But we do not want to have to edit our code every time we run it on a computer with a different number of processors.

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