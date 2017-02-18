## Analyzing Fork-Join Algorithms

As with any algorithm, a fork-join parallel algorithm should be correct and efficient. This section focuses on the latter even though the former should always be one’s first concern. For efficiency, we will focus on asymptotic bounds and analyzing algorithms that are not written in terms of a fixed number of processors. That is, just as the size of the problem _n_ will factor into the asymptotic running time, so will the number of processors _P_. The Task Parallel Library (and similar libraries in other languages) will give us an optimal expected-time bound for any _P_. This section explains what that bound is and what it means, but we will not discuss _how_ the library achieves it.

We then turn to discussing Amdahl’s Law, which analyzes the running time of algorithms that have both sequential parts and parallel parts. The key and depressing upshot is that programs with even a small sequential part quickly stop getting much benefit from running with more processors.

Finally, we discuss Moore’s “Law” in contrast to Amdahl’s Law. While Moore’s Law is also important for understanding the progress of computing power, it is not a mathematical theorem like Amdahl’s Law.

### 4.1 Work and Span

#### 4.1.1 Defining Work and Span

We define _T<sub>P</sub>_ to be the time a program/algorithm takes to run if there are _P_ processors available during its execution. For example, if a program was the only one running on a quad-core machine, we would be particularly interested in _T<sub>4</sub>_, but we want to think about _T<sub>P</sub>_ more generally. It turns out we will reason about the general _T<sub>P</sub>_ in terms of _T<sub>1</sub>_ and _T<sub>∞</sub>_:

 * _T<sub>1</sub>_ is called the _work_. By definition, this is how long it takes to run on one processor. More intuitively, it is just the total of all the running time of all the pieces of the algorithm: we have to do all the work before we are done, and there is exactly one processor (no parallelism) to do it. In terms of fork-join, we can think of _T<sub>1</sub>_ as doing one side of the fork and then the other, though the total  _T<sub>1</sub>_ does not depend on how the work is scheduled.
 * _T<sub>∞</sub>_ is called the _span_, though other common terms are the _critical path length_ or _computational depth_. By definition, this is how long it takes to run on an unlimited number of processors. Notice this is _not_ necessarily _O(1)_ time; the algorithm still needs to do the forking and combining of results. For example, under our model of computation — where creating a new thread and adding two numbers are both _O(1)_ operations — the algorithm we developed is asymptotically optimal with _T<sub>∞</sub> = Θ(log n)_ for an array of length _n_.

We need a more precise way of characterizing the execution of a parallel program so that we can describe and compute the work, _T<sub>1</sub>_, and the span, _T<sub>∞</sub>_. We will describe a program execution as a directed acyclic graph (dag) where:

 * Nodes are pieces of work the program performs. Each node will be a constant, i.e., _O(1)_, amount of work that is performed sequentially. So _T<sub>1</sub>_ is asymptotically just the number of nodes in the dag.
 * Edges represent that the source node must complete before the target node begins. That is, there is a _computational dependency_ along the edge. This idea lets us visualize _T<sub>∞</sub>_: with unlimited processors, we would immediately start every node as soon as its predecessors in the graph had finished. Therefore _T<sub>∞</sub>_ is just the length of the longest path in the dag.

Figure 6 (!!) shows an example dag and the longest path, which determines _T<sub>∞</sub>_.

If you have studied combinational hardware circuits, this model is strikingly similar to the dags that arise in that setting. For circuits, _work_ is typically called the _size_ of the circuit, (i.e., the amount of hardware) and _span_ is typically called the _depth_ of the circuit, (i.e., the time, in units of “gate delay,” to produce an answer).

With basic fork-join divide-and-conquer parallelism, the execution dags are quite simple: The _O(1)_ work to set up two smaller subproblems is one node in the dag. This node has two outgoing edges to two new nodes that start doing the two subproblems. (The fact that one subproblem might be done by the same thread is not relevant here. Nodes are not threads. They are _O(1)_ pieces of work.) The two subproblems will lead to their own dags. When we join on the results of the subproblems, that creates a node with incoming edges from the last nodes for the subproblems. This same node can do an _O(1)_ amount of work to combine the results. (If combining results is more expensive, then it needs to be represented by more nodes.)

Overall, then, the dag for a basic parallel reduction would look like this: (unnumbered figure !!)

The root node represents the computation that divides the array into two equal halves. The bottom node represents the computation that adds together the two sums from the halves to produce the final answer. The base cases represent reading from a one-element range assuming no sequential cut-off. A sequential cut-off “just” trims out levels of the dag, which removes most of the nodes but affects the dag’s longest path by “only” a constant amount. Note that this dag is a conceptual description of how a program executes; the dag is not a data structure that gets built by the program.

From the picture, it is clear that a parallel reduction is basically described by two balanced binary trees whose size is proportional to the input data size. Therefore _T<sub>1</sub>_ is _O(n)_ (there are approximately _2n_ nodes) and _T<sub>∞</sub>_ is _O(log n)_ (the height of each tree is approximately _log n_). For the particular reduction we have been studying — summing an array — Figure 7 (!!) visually depicts the work being done for an example with 8 elements. The work in the nodes in the top half is to create two subproblems. The work in the nodes in the bottom half is to combine two results.

The dag model of parallel computation is much more general than for simple fork-join algorithms. It describes all the work that is done and the earliest that any piece of that work could begin. To repeat, _T<sub>1</sub>_ and _T<sub>∞</sub>_ become simple graph properties: the number of nodes and the length of the longest path, respectively.

#### 4.1.2 Defining Speedup and Parallelism

Having defined work and span, we can use them to define some other terms more relevant to our real goal of reasoning about _T<sub>P</sub>_. After all, if we had only one processor then we would not study parallelism and having infinity processors is impossible.

We define the _speedup_ on _P_ processors to be _T<sub>1</sub>/T<sub>P</sub>_. It is basically the ratio of how much faster the program runs given the extra processors. For example, if _T<sub>1</sub>_ is 20 seconds and _T<sub>4</sub>_ is 8 seconds, then the speedup for _P_ = 4 is 2.5.

You might naively expect a speed-up of 4, or more generally _P_ for _T<sub>P</sub>_. In practice, such a _perfect speedup_ is rare due to several issues including the overhead of creating threads and communicating answers among them, memory-hierarchy issues, and the inherent computational dependencies related to the span. In the rare case that doubling _P_ cuts the running time in half (i.e., doubles the speedup), we call it _perfect linear speedup_. In practice, this is not the absolute limit; one can find situations where the speedup is even higher even though our simple computational model does not capture the features that could cause this.

It is important to note that reporting only _T<sub>1</sub>/T<sub>P</sub>_ can be “dishonest” in the sense that it often overstates the advantages of using multiple processors. The reason is that _T<sub>1</sub>_ is the time it takes to run the _parallel algorithm_ on one processor, but this algorithm is likely to be much slower than an algorithm designed sequentially. For example, if someone wants to know the benefits of summing an array with parallel fork-join, they probably are most interested in comparing _T<sub>P</sub>_ to the time for the sequential for-loop. If we call the latter _S_, then the ratio _S/_T<sub>P</sub>_ is usually the speed-up of interest and will be lower, due to constant factors like the time to create recursive tasks, than the definition of speed-up _T<sub>1</sub>/T<sub>P</sub>_. One measure of the overhead of using multiple threads is simply _T<sub>1</sub>/S_, which is usually greater than 1.

As a final definition, we call _T<sub>1</sub>/T<sub>∞</sub>_ the _parallelism_ of an algorithm. It is a measure of how much improvement one could possibly hope for since it should be at least as great as the speed-up for any _P_.  For our parallel reductions where the work is _Θ(n)_ and the span is _Θ(log n)_, the parallelism is _Θ(n / log n)_. In other words, there is exponential available parallelism (_n_ grows exponentially faster than_log n_), meaning with enough processors we can hope for an exponential speed-up over the sequential version.

#### 4.1.3 The Task Parallel Library Bound

Under some important assumptions we will describe below,  algorithms written using the Task Parallel Library, in particular the divide-and-conquer algorithms in these notes, have the following _expected_ time bound:

<center>_T<sub>P</sub>_ is _O(T<sub>1</sub>/P + T<sub>∞</sub>)_</center>

The bound is _expected_ because internally the library uses randomness, so the bound can be violated from “bad luck” but such “bad luck” is exponentially unlikely, so it simply will not occur in practice. This is exactly like the expected-time running-time guarantee for the sequential quicksort algorithm when a pivot element is chosen randomly. Because these notes do not describe the library’s implementation, we will not see where the randomness arises.

Notice that, ignoring constant factors, this bound is optimal: given only _P_ processors, no implementation can expect to do better than _T<sub>1</sub>/P_ or better than _T<sub>∞</sub>_. For small _P_, the term _T<sub>1</sub>/P_ is likely to be dominant and we can expect roughly linear speed-up. As _P_ grows, the span becomes more relevant and the limit on the run-time is more influenced by _T<sub>∞</sub>_.

Constant factors can be relevant, and it is entirely possible that a hand-crafted parallel algorithm in terms of some fixed _P_ could do better than a generic library that has no idea what sort of parallel algorithm it is running. But just like we often use asymptotically optimal data structures even if hand-crafted ones for our task might be a little faster, using a library such as this is often an excellent approach.

Thinking in terms of the program-execution dag, it is rather amazing that a library can achieve this optimal result. While the program is running, it is the library’s job to choose among all the threads that _could_ run next (they are not blocked waiting for some other thread to finish) and assign _P_ of them to processors. For simple parallel reductions, the choice hardly matters because all paths to the bottom of the dag are about the same length, but for arbitrary dags it seems important to work on the longer paths. Yet it turns out a much greedier algorithm that just picks randomly among the available threads will do only a constant factor worse. But this is all about the library’s internal scheduling algorithm (which is not actually totally random) and we, as library users, can just rely on the provided bound.

However, as mentioned above, the bound holds only under a couple assumptions.  The first is that all the threads you create to do subproblems do approximately the same amount of work. Otherwise, if a thread with much-more-work-to-do is scheduled very late, other processors will sit idle waiting for this laggard to finish. The second is that all the threads do a small but not tiny amount of work. In other words, just avoid threads that do millions of operations as well as threads that do dozens.

To summarize, as a user of a library like this, your job is to pick a good parallel algorithm, implement it in terms of divide-and-conquer with a reasonable sequential cut-off, and analyze the expected run-time in terms of the provided bound. The library’s job is to give this bound while trying to maintain low constant-factor overheads. While this library is particularly good for _this_ style of programming, this basic division is common: application writers develop good algorithms and rely on some underlying thread scheduler to deliver reasonable performance.