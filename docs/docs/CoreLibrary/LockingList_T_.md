### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## LockingList&lt;T&gt; Class
A wrapper for the List class that locks the list on any modification operations.  
```csharp
public class LockingList<T> :
System.Collections.Generic.ICollection<T>,
System.Collections.Generic.IEnumerable<T>,
System.Collections.IEnumerable,
System.Collections.Generic.IList<T>,
System.Collections.Generic.IReadOnlyCollection<T>,
System.Collections.Generic.IReadOnlyList<T>
```
#### Type parameters
<a name='Microsoft_FactoryOrchestrator_Core_LockingList_T__T'></a>
`T`  
  

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; LockingList&lt;T&gt;  

Implements [System.Collections.Generic.ICollection&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.ICollection-1 'System.Collections.Generic.ICollection')[T](LockingList_T_.md#Microsoft_FactoryOrchestrator_Core_LockingList_T__T 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.T')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.ICollection-1 'System.Collections.Generic.ICollection'), [System.Collections.Generic.IEnumerable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable')[T](LockingList_T_.md#Microsoft_FactoryOrchestrator_Core_LockingList_T__T 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.T')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable'), [System.Collections.IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.IEnumerable 'System.Collections.IEnumerable'), [System.Collections.Generic.IList&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IList-1 'System.Collections.Generic.IList')[T](LockingList_T_.md#Microsoft_FactoryOrchestrator_Core_LockingList_T__T 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.T')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IList-1 'System.Collections.Generic.IList'), [System.Collections.Generic.IReadOnlyCollection&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyCollection-1 'System.Collections.Generic.IReadOnlyCollection')[T](LockingList_T_.md#Microsoft_FactoryOrchestrator_Core_LockingList_T__T 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.T')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyCollection-1 'System.Collections.Generic.IReadOnlyCollection'), [System.Collections.Generic.IReadOnlyList&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyList-1 'System.Collections.Generic.IReadOnlyList')[T](LockingList_T_.md#Microsoft_FactoryOrchestrator_Core_LockingList_T__T 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.T')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyList-1 'System.Collections.Generic.IReadOnlyList')  

| Properties | |
| :--- | :--- |
| [Count](LockingList_T__Count.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.Count') | Gets the number of elements contained in the [System.Collections.ICollection](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.ICollection 'System.Collections.ICollection').<br/> |
| [IsFixedSize](LockingList_T__IsFixedSize.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.IsFixedSize') | Gets a value indicating whether this instance is fixed size.<br/> |
| [IsReadOnly](LockingList_T__IsReadOnly.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.IsReadOnly') | Gets a value indicating whether the [System.Collections.ICollection](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.ICollection 'System.Collections.ICollection') is read-only.<br/> |
| [IsSynchronized](LockingList_T__IsSynchronized.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.IsSynchronized') | Gets a value indicating whether this instance is synchronized.<br/> |
| [SyncRoot](LockingList_T__SyncRoot.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.SyncRoot') | Gets the synchronize root.<br/> |
| [this[int]](LockingList_T__this_int_.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.this[int]') | Gets or sets the item T at the specified index.<br/> |

| Methods | |
| :--- | :--- |
| [Add(T)](LockingList_T__Add(T).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.Add(T)') | Adds an item to the [System.Collections.ICollection](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.ICollection 'System.Collections.ICollection').<br/> |
| [AddRange(IEnumerable&lt;T&gt;)](LockingList_T__AddRange(IEnumerable_T_).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.AddRange(System.Collections.Generic.IEnumerable&lt;T&gt;)') | Adds the elements of the specified collection to the end of the System.Collections.Generic.List`1.<br/> |
| [Clear()](LockingList_T__Clear().md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.Clear()') | Removes all items from the [System.Collections.ICollection](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.ICollection 'System.Collections.ICollection').<br/> |
| [Contains(T)](LockingList_T__Contains(T).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.Contains(T)') | Determines whether this instance contains the object.<br/> |
| [CopyTo(T[], int)](LockingList_T__CopyTo(T___int).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.CopyTo(T[], int)') | Copies the elements of the [System.Collections.ICollection](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.ICollection 'System.Collections.ICollection') to an [System.Array](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array'), starting at a particular [System.Array](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array') index.<br/> |
| [GetEnumerator()](LockingList_T__GetEnumerator().md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.GetEnumerator()') | Returns an enumerator that iterates through the collection.<br/> |
| [GetRange(int, int)](LockingList_T__GetRange(int_int).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.GetRange(int, int)') | Creates a shallow copy of a range of elements in the source System.Collections.Generic.List`1.<br/> |
| [IndexOf(T)](LockingList_T__IndexOf(T).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.IndexOf(T)') | Determines the index of a specific item in the [System.Collections.IList](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.IList 'System.Collections.IList').<br/> |
| [Insert(int, T)](LockingList_T__Insert(int_T).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.Insert(int, T)') | Inserts an item to the [System.Collections.IList](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.IList 'System.Collections.IList') at the specified index.<br/> |
| [Remove(T)](LockingList_T__Remove(T).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.Remove(T)') | Removes the first occurrence of a specific object from the [System.Collections.ICollection](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.ICollection 'System.Collections.ICollection').<br/> |
| [RemoveAt(int)](LockingList_T__RemoveAt(int).md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;.RemoveAt(int)') | Removes the [System.Collections.IList](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.IList 'System.Collections.IList') item at the specified index.<br/> |
#### See Also
- [System.Collections.Generic.ICollection&lt;&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.ICollection-1 'System.Collections.Generic.ICollection')
- [System.Collections.Generic.IEnumerable&lt;&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable')
- [System.Collections.IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.IEnumerable 'System.Collections.IEnumerable')
- [System.Collections.Generic.IList&lt;&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IList-1 'System.Collections.Generic.IList')
- [System.Collections.Generic.IReadOnlyCollection&lt;&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyCollection-1 'System.Collections.Generic.IReadOnlyCollection')
- [System.Collections.Generic.IReadOnlyList&lt;&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyList-1 'System.Collections.Generic.IReadOnlyList')
