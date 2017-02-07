## 2 Introduction

### 2.1 More Than One Thing At Once

In _sequential programming_, one thing happens at a time. Sequential programming is what most people learn first and how most programs are written. Probably every program you have written in C# (or a similar language) is sequential: execution starts at the beginning of `main` and proceeds one assignment / call / return / arithmetic operation at a time.

Removing the one-thing-at-a-time assumption complicates writing software.  The multiple _threads of execution_ (things performing computations) will somehow need to coordinate so that they can work together to complete a task — or at least not get in each other’s way while they are doing separate things. These notes cover basic concepts related to _multithreaded programming_ , i.e., programs where there are multiple threads of execution. We will cover:
* How to create multiple threads
* How to write and analyze divide-and-conquer algorithms that use threads to produce results more quickly
* How to coordinate access to shared objects so that multiple threads using the same data do not produce the wrong answer

A useful analogy is with cooking. A sequential program is like having one cook who does each step of a recipe in order, finishing one step before starting the next. Often there are multiple steps that could be done at the same time — if you had more cooks.  But having more cooks requires extra coordination.  One cook may have to wait for another cook to finish something. And there are limited resources: if you have only one oven, two cooks won’t be able to bake casseroles at different temperatures at the same time. In short, multiple cooks present efficiency opportunities, but also significantly complicate the process of producing a meal.

Because multithreaded programming is so much more difficult, it is best to avoid it if you can. For most of computing’s history, most programmers wrote only sequential programs. Notable exceptions were:
* Programmers writing programs to solve such computationally large problems that it would take years or centuries for one computer to finish. So they would use multiple computers together.
* Programmers writing systems like an operating system where a key point of the system is to handle multiple things happening at once.  For example, you can have more than one program running at a time.  If you have only one processor, only one program can _actually_ run at a time, but the operating system still uses threads to keep track of all the running programs and let them take turns. If the taking turns happens fast enough (e.g., 10 milliseconds), humans fall for the illusion of simultaneous execution. This is called _time-slicing_.

Sequential programmers were lucky:  since every 2 years or so computers got roughly twice as fast, most programs would get exponentially faster over time without any extra effort.

Around 2005, computers stopped getting twice as fast every 2 years. To understand why requires a course in computer architecture. In brief, increasing the clock rate (very roughly and technically inaccurately speaking, how quickly instructions execute) became infeasible without generating too much heat.  Also, the relative cost of memory accesses can become too high for faster processors to help.

Nonetheless, chip manufacturers still plan to make exponentially more powerful chips.  Instead of one processor running faster, they will have more processors. The next computer you buy will likely have 4 processors (also called _cores_) on the same chip and the number of available cores will likely double every few years.

What would 256 cores be good for? Well, you can run multiple programs at once — for real, not just with time-slicing. But for an individual program to run any faster than with one core, it will need to do more than one thing at once. This is the reason that multithreaded programming is becoming more important. To be clear, _multithreaded programming is not new. It has existed for decades and all the key concepts are just as old._ Before there were multiple cores on one chip, you could use multiple chips and/or use time-slicing on one chip — and both remain important techniques today. The move to multiple cores on one chip is “just” having the effect of making multithreading something that more and more software wants to do.

### 2.2 Parallelism vs. Concurrency

These notes are organized around a fundamental distinction between _parallelism_ and _concurrency_. Unfortunately, the way we define these terms is not entirely standard, so you should not assume that everyone uses these terms as we will. Nonetheless, most computer scientists agree that this distinction is important.

__Parallel programming is about using additional computational resources to produce an answer faster.__

As a canonical example, consider the trivial problem of summing up all the numbers in an array. We know no sequential algorithm can do better than _$$\theta(n)$$_ time. Suppose instead we had 4 processors. Then hopefully we could produce the result roughly 4 times faster by having each processor add 1/4 of the elements and then we could just add these 4 partial results together with 3 more additions. _$$\theta(n/4)$$_ is still _$$\theta(n)$$_, but constant factors can matter. Moreover, when designing and analyzing a _parallel algorithm_, we should leave the number of processors as a variable, call it _P_. Perhaps we can sum the elements of an array in time _O(n/P)_ given _P_ processors. As we will see, in fact the best bound under the assumptions we will make is _O(log n + n/P)_.

In terms of our cooking analogy, parallelism is about using extra cooks (or utensils or pans or whatever) to get a large meal finished in less time. If you have a huge number of potatoes to slice, having more knives and people is really helpful, but at some point adding more people stops helping because of all the communicating and coordinating you have to do: it is faster for me to slice one potato by myself than to slice it into fourths, give it to four other people, and collect the results.

__Concurrent programming is about correctly and efficiently controlling access by multiple threads to shared resources.__

As a canonical example, suppose we have a dictionary implemented as a hashtable with operations `insert`, `lookup`, and `delete`. Suppose that inserting an item already in the table is supposed to update the key to map to the newly inserted value. Implementing this data structure for sequential programs is something we assume you could already do correctly.  Now suppose different threads use the _same_ hashtable, potentially at the same time. Suppose two threads even try to `insert` the same key at the same time. What might happen? You would have to look at your sequential code carefully, but it is entirely possible that the same key might end up in the table twice. That is a problem since a subsequent `delete` with that key might remove only one of them, leaving the key in the dictionary.

To prevent problems like this, concurrent programs use _synchronization primitives_ to prevent multiple threads from _interleaving their operations_ in a way that leads to incorrect results.  Perhaps a simple solution in our hashtable example is to make sure only one thread uses the table at a time, finishing an operation before another thread starts. But if the table is large, this is unnecessarily inefficient most of the time if the threads are probably accessing different parts of the table.

In terms of cooking, the shared resources could be something like an oven.  It is important not to put a casserole in the oven unless the oven is empty. If the oven is not empty, we could keep checking until it is empty. In C#, you might naively write:

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