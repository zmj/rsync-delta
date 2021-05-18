# Rsync.Delta

This library is a C# (netstandard2.0) implementation of librsync's signature, delta, and patch operations: https://librsync.github.io/page_rdiff.html

Available on NuGet: https://www.nuget.org/packages/Rsync.Delta/

### Use Case

Differential sync trades reduced bandwidth consumption for increased CPU usage when transferring similar files between two computers. Only the difference between the two files is sent across the network. A common scenario is keeping different versions of the same file in sync across two or more computers.

### Comparison to librsync

Librsync is the authoritative implementation of this algorithm. This library implements that algorithm on top of .NET IO abstractions: Streams and Pipes. This enables the algorithm to operate on network inputs / outputs, instead of creating temporary files and copying their content.

If librsync fits your use case, use it instead of this library. Librsync is higher quality and more performant for most scenarios that it supports.
