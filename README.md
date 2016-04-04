# Invertible Bloom Filters
On using invertible Bloom filters for estimating differences between sets of key/value pairs. Written in C#.

The goal is to efficiently determine the differences between two sets of key/value pairs. 

The first approach is based upon invertible Bloom filters, as described in "What’s the Difference? Efﬁcient Set Reconciliation without Prior Context" (David Eppstein, Michael T. Goodrich, Frank Uyeda, George Varghese, 2011, http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf) . A similar data structure, but not extended to detect set differences, is described in "Invertible Bloom Lookup Tables" (Michael T. Goodrich, Michael Mitzenmacher, 2015, http://arxiv.org/pdf/1101.2245v3.pdf). 

After implementing the data structure as described in the paper, it was noted that the data structure detected changes in the keys, but did not perform equally well at detecting changes in the values. An alternative solution is presented in the form of a reverse invertible Bloom filter.

Included with the Bloom Filter is a strata estimator (as described in the above paper). Based upon the strata estimator, a hybrid strata estimator was implemented utilizing the b-bit minwise hash (described in http://research.microsoft.com/pubs/120078/wfc0398-liPS.pdf). 

The estimator is important, because an estimate of the number of differences is needed to pick a proper sized Bloom filter that can be decoded. The size of the invertible Bloom filter needed for detecting the changes between two sets is not dependent upon the set sizes, but upon the size of the difference.  For example, 30 differences between two sets of 500000 elements each can be fully detected by 63 kilobyte invertible Bloom filter. On the other hand, 40000 differences between two sets of 45000 items each can take a Bloom filter of 17 megabytes. 

When the estimate for the difference is too large, a Bloom filter will be used that requires more space than needed. When the estimate is too small, a Bloom filter might be used that can't be successfully decoded, additional space and time is required to find a Bloom filter that is large enough to be succesfully decoded.

## Serialization
Support has been added for serializing and deserializing Bloom filters and estimators.

## Overloading a Bloom filter
When utilizing an invertible Bloom filter within the capacity it was sized for, the count will seldom exceed 2 or 3. However, when utilizing estimators, the idea is that the invertible Bloom filter will be utilized at a much higher capacity than it was sized for, thus accepting a higher error rate and much higher count values. To account for both scenario's, the actual count type is configurable. Two types will be supported out of the box: sbyte and int.

## Computational overhead
The cost of computing the value hash can be considerable. If this becomes an issue, a dictionary of keys and pre-computed hash values can be maintained (for example in a NoSQL store). An example of the key-value Bloom filter configuration needed to utilize these precomputed values has been included.

The estimators and Bloom filters themselves can in fact be pre-computed under certain conditions. An estimator can be fully serialized and deserialized for storage. However, since an estimator does not know a Remove operation, an estimator would have to be recomputed after a number of updates or deletes have occured or when the set size becomes significantly larger than the set size that the estimator was sized for. Bloom filters do support deletes (and updates through a combined delete/insert), and thus can be kept in-sync with the data. A number of different sized Bloom filters could be pre-computed and picked from based upon the estimate of the difference. When a comparable sized Bloom filter is not available, you could either give it a best effort (assuming some differences will be recognized, thus reducing the overall number of differences and making progress) or calculate a matching Bloom filter. Note that the size of the pre-calculated estimators and Bloom filters should be agreed upon by the parties involved, since estimators or Bloom filters that have no factors in common, can't be compared.

A practial scenario would be that all parties agree upon a pre-determined size for the estimator and the filter, typically larger than the maximum size anticipated. The following scenario's are possible:
- The first estimator has at least the required capacity and strata count needed by the receiver to represent the set. The two estimators will decode correctly and provide an estimate.
- The first estimator sent does not have a large enough capacity or large enough strata for the receiver their dataset. The receiver has two options:
    - Send their estimator back. This estimator will have the capacity needed.
    - Assume that a failed decode means that the difference is large and close to the total item count of the Bloom filter. You can choose to send the whole Bloom filter (possibly still trading recognized differences versus some level of folding).

When the two estimators provide an estimate, you can send the local Bloom filter, folded to be in the order of size of the estimate. When the two filters cannot be subtracted and decoded, the filters are not compatible. The parties should agree on a new precalculated filter size. It is important to remember in this process that:
- Estimators are helpful in avoiding over-sized Bloom filters, but you can exchange Bloom filters even without an estimate. There is just a higher risk of either wasting band width or only recognizing a relatively small fraction of the differences.
- Bloom filters that are not under sized will still yield some differences, and thus provide progress toward eliminating the differences.
- The only major obstacle occurs when the Bloom filters are not compatible, for example because they can't be resized to have the same size (no shared factor), have different hash functions or have different hash counts. A minor obstacle is two estimators that do not have a shared factor, but you could still choose to exchange the Bloom filters.

## Resizing a Bloom filter
Bloom filters can be folded through any factor of its size. A strategy is provided for folding, including a strategy that sizes Bloom filter with smooth numbers to increase the folding opportunities. Based upon the folding operator, a compression operator is provided that attempts to reduce the size of a Bloom filter utilized under its capacity without impacting its error rate. The same operators are also provided for the minwise estimator.

## Wishlist
Although this is initially just a testbed, an obvious wishlist item is a buffer pool to counteract some of the horrible things the Bloom Filter does to memory management.

