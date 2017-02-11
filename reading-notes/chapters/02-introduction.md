## 2 Introduction

### 2.1 More Than One Thing At Once

In _sequential programming_, one thing happens at a time. Sequential programming is what most people learn first and how most programs are written. Probably every program you have written in C# (or a similar language) is sequential: execution starts at the beginning of `main` and proceeds one assignment / call / return / arithmetic operation at a time.

Removing the one-thing-at-a-time assumption complicates writing software. The multiple _threads of execution_ (things performing computations) will somehow need to coordinate so that they can work together to complete a task — or at least not get in each other’s way while they are doing separate things. These notes cover basic concepts related to _multithreaded programming_ , i.e., programs where there are multiple threads of execution. We will cover:

* How to create multiple threads
* How to write and analyze divide-and-conquer algorithms that use threads to produce results more quickly
* How to coordinate access to shared objects so that multiple threads using the same data do not produce the wrong answer

A useful analogy is with cooking. A sequential program is like having one cook who does each step of a recipe in order, finishing one step before starting the next. Often there are multiple steps that could be done at the same time — if you had more cooks. But having more cooks requires extra coordination. One cook may have to wait for another cook to finish something. And there are limited resources: if you have only one oven, two cooks won’t be able to bake casseroles at different temperatures at the same time. In short, multiple cooks present efficiency opportunities, but also significantly complicate the process of producing a meal.

Because multithreaded programming is so much more difficult, it is best to avoid it if you can. For most of computing’s history, most programmers wrote only sequential programs. Notable exceptions were:

* Programmers writing programs to solve such computationally large problems that it would take years or centuries for one computer to finish. So they would use multiple computers together.
* Programmers writing systems like an operating system where a key point of the system is to handle multiple things happening at once. For example, you can have more than one program running at a time. If you have only one processor, only one program can _actually_ run at a time, but the operating system still uses threads to keep track of all the running programs and let them take turns. If the taking turns happens fast enough (e.g., 10 milliseconds), humans fall for the illusion of simultaneous execution. This is called _time-slicing_.

Sequential programmers were lucky: since every 2 years or so computers got roughly twice as fast, most programs would get exponentially faster over time without any extra effort.

Around 2005, computers stopped getting twice as fast every 2 years. To understand why requires a course in computer architecture. In brief, increasing the clock rate (very roughly and technically inaccurately speaking, how quickly instructions execute) became infeasible without generating too much heat. Also, the relative cost of memory accesses can become too high for faster processors to help.

Nonetheless, chip manufacturers still plan to make exponentially more powerful chips. Instead of one processor running faster, they will have more processors. The next computer you buy will likely have 4 processors (also called _cores_) on the same chip and the number of available cores will likely double every few years.

What would 256 cores be good for? Well, you can run multiple programs at once — for real, not just with time-slicing. But for an individual program to run any faster than with one core, it will need to do more than one thing at once. This is the reason that multithreaded programming is becoming more important. To be clear, _multithreaded programming is not new. It has existed for decades and all the key concepts are just as old._ Before there were multiple cores on one chip, you could use multiple chips and/or use time-slicing on one chip — and both remain important techniques today. The move to multiple cores on one chip is “just” having the effect of making multithreading something that more and more software wants to do.

### 2.2 Parallelism vs. Concurrency

These notes are organized around a fundamental distinction between _parallelism_ and _concurrency_. Unfortunately, the way we define these terms is not entirely standard, so you should not assume that everyone uses these terms as we will. Nonetheless, most computer scientists agree that this distinction is important.

__Parallel programming is about using additional computational resources to produce an answer faster.__

As a canonical example, consider the trivial problem of summing up all the numbers in an array. We know no sequential algorithm can do better than _$$\theta(n)$$_ time. Suppose instead we had 4 processors. Then hopefully we could produce the result roughly 4 times faster by having each processor add 1/4 of the elements and then we could just add these 4 partial results together with 3 more additions. _$$\theta(n/4)$$_ is still _$$\theta(n)$$_, but constant factors can matter. Moreover, when designing and analyzing a _parallel algorithm_, we should leave the number of processors as a variable, call it _P_. Perhaps we can sum the elements of an array in time _O(n/P)_ given _P_ processors. As we will see, in fact the best bound under the assumptions we will make is _O(log n + n/P)_.

In terms of our cooking analogy, parallelism is about using extra cooks (or utensils or pans or whatever) to get a large meal finished in less time. If you have a huge number of potatoes to slice, having more knives and people is really helpful, but at some point adding more people stops helping because of all the communicating and coordinating you have to do: it is faster for me to slice one potato by myself than to slice it into fourths, give it to four other people, and collect the results.

__Concurrent programming is about correctly and efficiently controlling access by multiple threads to shared resources.__

As a canonical example, suppose we have a dictionary implemented as a hashtable with operations `insert`, `lookup`, and `delete`. Suppose that inserting an item already in the table is supposed to update the key to map to the newly inserted value. Implementing this data structure for sequential programs is something we assume you could already do correctly. Now suppose different threads use the _same_ hashtable, potentially at the same time. Suppose two threads even try to `insert` the same key at the same time. What might happen? You would have to look at your sequential code carefully, but it is entirely possible that the same key might end up in the table twice. That is a problem since a subsequent `delete` with that key might remove only one of them, leaving the key in the dictionary.

To prevent problems like this, concurrent programs use _synchronization primitives_ to prevent multiple threads from _interleaving their operations_ in a way that leads to incorrect results. Perhaps a simple solution in our hashtable example is to make sure only one thread uses the table at a time, finishing an operation before another thread starts. But if the table is large, this is unnecessarily inefficient most of the time if the threads are probably accessing different parts of the table.

In terms of cooking, the shared resources could be something like an oven. It is important not to put a casserole in the oven unless the oven is empty. If the oven is not empty, we could keep checking until it is empty. In C#, you might naively write:

    while (true)
    {
        if (ovenIsEmpty())
        {
            putCasseroleInOven();
            break;
        }
    }

Unfortunately, code like this is broken if two threads run it at the same time, which is the primary complication in concurrent programming. They might both see an empty oven and then both put a casserole in. We will need to learn ways to check the oven and put a casserole in without any other thread doing something with the oven in the meantime.

__Comparing Parallelism and Concurrency__

We have emphasized here how parallel programming and concurrent programming are different. Is the problem one of using extra resources effectively or is the problem one of preventing a bad interleaving of operations from different threads? It is all-too-common for a conversation to become muddled because one person is thinking about parallelism while the other is thinking about concurrency.

In practice, the distinction between parallelism and concurrency is not absolute. Many programs have aspects of each. Suppose you had a huge array of values you wanted to insert into a hash table. From the perspective of dividing up the insertions among multiple threads, this is about parallelism. From the perspective of coordinating access to the hash table, this is about concurrency. Also, parallelism does typically need some coordination: even when adding up integers in an array we need to know when the different threads are done with their chunk of the work.

We believe parallelism is an easier concept to start with than concurrency. You probably found it easier to understand how to use parallelism to add up array elements than understanding why the while-loop for checking the oven was wrong. (And if you still don’t understand the latter, don’t worry, later sections will explain similar examples line-by-line.) So we will start with parallelism (Sections 3, 4, 5), getting comfortable with multiple things happening at once. Then we will switch our focus to concurrency (Sections 6, 7, 8, 9, 10) and shared resources (using memory instead of ovens), learn many of the subtle problems that arise, and present programming guidelines to avoid them.

### Basic Threads and Shared Memory

Before writing any parallel or concurrent programs, we need some way to _make multiple things happen at once_ and some way for those different things to _communicate_. Put another way, your computer may have multiple cores, but all the Java constructs you know are for sequential programs, which do only one thing at once. Before showing any Java specifics, we need to explain the _programming model_.


The model we will assume is _explicit threads_ with _shared memory_. A _thread_ is itself like a running sequential program, but one thread can create other threads that are part of the same program and those threads can create more threads, etc. Two or more threads can communicate by writing and reading fields of the same objects. In other words, they share memory. This is only one model of parallel/concurrent programming, but it is the only one we will use. The next section briefly mentions other models that a full course on parallel/concurrent programming would likely cover.

Conceptually, all the threads that have been started but not yet terminated are “running at once” in a program. In practice, they may not all be running at any particular moment:

* There may be more threads than processors. It is up to the Java implementation, with help from the underlying operating system, to find a way to let the threads “take turns” using the available processors. This is called _scheduling_ and is a major topic in operating systems. All we need to know is that it is not under the Java programmer’s control: you create the threads and the system schedules them.
* A thread may be waiting for something to happen before it continues. For example, the next section discusses the `join` primitive where one thread does not continue until another thread has terminated.

Let’s be more concrete about what a thread is and how threads communicate. It is helpful to start by enumerating the key pieces that a _sequential_ program has _while it is running_ (see also Figure 1 - !!insert image!!):

1. One _call stack_ , where each _stack frame_ holds the local variables for a method call that has started but not yet finished. Calling a method pushes a new frame and returning from a method pops a frame. Call stacks are why recursion is not “magic”.
2. One _program counter_ . This is just a low-level name for keeping track of what statement is currently executing. In a sequential program, there is exactly one such statement.
3. Static fields of classes.
4. Objects. An object is created by calling `new`, which returns a reference to the new object. We call the memory that holds all the objects the _heap_. This use of the word “heap” has nothing to do with heap data structure used to implement priority queues. It is separate memory from the memory used for the call stack and static fields.

With this overview of the sequential _program state_, it is much easier to understand threads:

__Each thread has its own call stack and program counter, but all the threads share one collection of static fields and objects.__ (See also Figure 2. - !!insert image!!)

* When a new thread starts running, it will have its own new call stack. It will have one frame on it, which is _like_ that thread’s `main` , but it won’t actually be `main`.
* When a thread returns from its first method, it terminates.
* Each thread has its own program counter and local variables, so there is no “interference” from other threads for these things. The way loops, calls, assignments to variables, exceptions, etc. work for each thread is just like you learned in sequential programming and is separate for each thread.
* What is different is how static fields and objects work. In sequential programming we know `x.f = 42; y = x.f;` always assigns `42` to the variable `y`. But now the object that `x` refers to might also have its `f` field written to by other threads, so we cannot be so sure.

In practice, even though all objects _could_ be shared among threads, most are not. In fact, just as having static fields is often poor style, having lots of objects shared among threads is often poor style. But we need _some_ shared objects because that is how threads communicate. If we are going to create parallel algorithms where helper threads run in parallel to compute partial answers, they need some way to communicate those partial answers back to the “main” thread. The way we will do it is to have the helper threads write to some object fields that the main thread later reads.

We finish this section with some C# specifics for exactly how to create a new thread in C#. The details vary in different languages and in fact the parallelism portion of these notes mostly uses a different C# library with slightly different specifics. In addition to creating threads, we will need other language constructs for coordinating them. For example, for one thread to read the result another thread wrote as its answer, the reader often needs to know the writer is done. We will present such primitives as we need them.

To create a new thread in C# requires that you define a new class (step 1) and then perform two actions at run-time (steps 2–3):

1. Define a method that takes does the intended work and returns `void`. It must take no arguments, but the example below shows how to work around this inconvenience. Let's call this method `Run`.
2. Create an instance of the class `System.Threading.Thread` and pass a defined method `Run` as an argument. Notice that your method is an implementation of a delegate `System.Threading.ThreadStart` which `Thread` constructor expects. Also note that this does _not_ yet create a running thread. It just creates an object of class `Thread`. 
3. Call the `Start` method of the object you created in step 2. This step does the “magic” creation of a new thread. That new thread will execute the method that you defined in step 1. Notice that you do _not_ call the method `Run` itself; that would just be an ordinary method call. You call `Start`, which makes a new thread that runs it. The call to `Start` “returns immediately” so the caller continues on, in parallel with the newly-created thread running your method. The new thread terminates when its execution completes.

Here is a complete example of a useless C# program that starts with one thread and then creates 20 more threads:

    class ExampleThread
    {
        static void Run(int i)
        {
            Console.WriteLine("Thread {0} says hi", i);
            Console.WriteLine("Thread {0} says bye", i);
        }

        static void Main(string[] args)
        {
            for (int i = 1; i <= 20; i++)
            {
                int j = i;
                Thread t = new Thread(() => Run(j));
                t.Start();
            }
        }
    }

When this program runs, it will print 40 lines of output, one of which is:

    Thread 13 says hi

Interestingly, we cannot predict the order for these 40 lines of output. In fact, if you run the program multiple times, you will probably see the output appear in different orders on different runs. After all, each of the 21 separate threads running “at the same time” (conceptually, since your machine may not have 21 processors available for the program) can run in an unpredictable order. The main thread is the first thread and then it creates 20 others. The main thread always creates the other threads in the same order, but it is up to the Java implementation to let all the threads “take turns” using the available processors. There is no guarantee that threads created earlier run earlier. Therefore, multi-threaded programs are often `nondeterministic`, meaning their output can change even if the input does not. This is a main reason that multithreaded programs are more difficult to test and debug. Figure 3 (!!insert image!!) shows two possible orders of execution, but there are many, many more.

So is any possible ordering of the 40 output lines possible? No. Each thread still runs sequentially. So we will always see `Thread 13 says hi` _before_ the line `Thread 13 says bye` even though there may be other lines in-between. We might also wonder if two lines of output would ever be mixed, something like:

    Thread 13 Thread says 14 says hi hi

This is really a question of how the `Console.WriteLine` method handles concurrency and the answer happens to be that it will always keep a line of output together, so this would not occur. In general, concurrency introduces new questions about how code should and does behave.

We can also see how the example worked around the rule that `Run` cannot take any arguments. The standard idiom is to wrap a call to `Run` into a call of a different method that takes no argumments, and use closure to pass any “arguments” for the new thread to `Run`.

### 2.4 Other Models

While these notes focus on using threads and shared memory to handle parallelism and concurrency, it would be misleading to suggest that this is the only model for parallel/concurrent programming. Shared memory is often considered _convenient_ because communication uses “regular” reads and writes of fields to objects. However, it is also considered _error-prone_ because communication is implicit; it requires deep understanding of the code/documentation to know which memory accesses are doing inter-thread communication and which are not. The definition of shared-memory programs is also much more subtle than many programmers think because of issues regarding _data races_, as discussed in Section 7.

Here are three well-known, popular alternatives to shared memory. As is common in computer science, no option is “clearly better”. Different models are best-suited to different problems, and any model can be abused to produce incorrect or unnecessarily complicated software. One can also build abstractions using one model on top of another model, or use multiple models in the same program. These are really different perspectives on how to describe parallel/concurrent programs.

__Message-passing__ is the natural alternative to shared memory. In this model, we have explicit threads, but they do not share objects. To communicate, there is a separate notion of a _message_, which sends a _copy_ of some data to its recipient. Since each thread has its own objects, we do not have to worry about other threads wrongly updating fields. But we do have to keep track of different copies of things being produced by messages. When processors are far apart, message passing is likely a more natural fit, just like when you send email and a copy of the message is sent to the recipient. Here is a visual interpretation of message-passing: (!!image here!!)

__Dataflow__ provides more structure than having “a bunch of threads that communicate with each other however they want.” Instead, the programmer uses primitives to create a directed acyclic graph (DAG). A node in the graph performs some computation using inputs that arrive on its incoming edges. This data is provided by other nodes along their outgoing edges. A node starts computing when all of its inputs are available, something the implementation keeps track of automatically. Here is a visual interpretation of dataflow where different nodes perform different operations for some computation, such as “filter,” “fade in,” and “fade out”: (!!image here!!)

__Data parallelism__ does not have explicit threads or nodes running different parts of the program at different times. Instead, it has primitives for parallelism that involve applying the _same_ operation to different pieces of data at the same time. For example, you would have a primitive for applying some function to every element of an array. The implementation of this primitive would use parallelism rather than a sequential for-loop. Hence all the parallelism is done for you provided you can express your program using the available primitives. Examples include vector instructions on some processors and map-reduce style distributed systems. Here is a visual interpretation of data parallelism: (!!image here!!)
