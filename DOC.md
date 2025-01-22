# Webpack Chunk Loader

- [Loader Detection](#loader-detection)
- [Chunk ID Detection](#chunk-id-detection)
  - [Different Matchers](#different-matchers)
  - [Chunk ID Type Precaution](#chunk-id-type-precaution)
  - [Extraction](#extraction)
- [Chunk Filename Extraction](#chunk-filename-extraction)
- [Webpack Site](#webpack-site)

There are a few characteristics of a Webpack chunk loader:
- They usually appear inside the "main" `.js` file
- There can be multiple chunk loaders for a site
- They are either a `function(e)` or `e => {}`
- They return a string containing the path to the chunk `.js` file
  - Hence there must be one or more string literals that ends with `.js`
- They take exactly 1 argument (usually named `e`) which is the chunk ID
- They match the chunk ID by using object (for example, `{1:'chunk'}[e] + '.js'`) and using `if`s and ternary operators
  - They might also use `switch` however I have not yet encountered any in the wild
- They don't have any loops or function calls, but they might reference a field (for example, `a.p`)

A typical chunk loader looks like this:
```js
e => ({
  400: "chunk2",
  411: "chunk1",
  826: "chunkBtn"
})[e] + "." + {
  400: "5bc7d56beddd675ee02a",
  411: "bee97256afba34de130e",
  826: "c2ce9ce9700e2a0ab724"
}[e] + ".js"
```
And when passed in `400`, the loader returns `chunk2.5bc7d56beddd675ee02a.js`. Passing other chunk IDs return the other chunk paths.

## Loader Detection
From the loader characteristics I created a simple detection algorithm:
```
FOR node IN script:
    IF node is not function or arrow function: skip
    IF function parameter count is not 1: skip
    IF function contains loop/function calls: skip
    IF there are no literals ending with .js: skip

    add node to loaders
```
This works for most of cases however there might be some false positives (non-loader functions) returned. Next step is to extract all the chunk IDs and run the loader using them, from there we can determine the loader more accurately.

## Chunk ID Detection
I have seen a few different cases of how the loader function matches the chunk ID. In most cases the chunk ID is an int, however it can also be a string (happens when you set `optimization: {chunkIds: "named"}`).

### Different Matchers
The matchings can be done using an object:
```js
e => e + "." + {
  1: "f1195e719ca60f6a72c0",
  2: "465e8bbaa66177c00940",
  3: "13714958953ee2c9b344",
  4: "dca13bd1686fbf6af19b"
}[e] + ".js"
```
Or using `if`:
```js
e => {
  if (1 === e) return "5bc7d56beddd675ee02a.js";
  if (2 === e) return "bee97256afba34de130e.js";
  if (3 === e) return "c2ce9ce9700e2a0ab724.js";
}
```
Or using ternary:
```js
e => (
  1 === e ? "5bc7d56beddd675ee02a.js" :
    2 === e ? "bee97256afba34de130e.js" :
      3 === e ? "c2ce9ce9700e2a0ab724.js" : void 0
)
```
Or a mix:
```js
function(e) {
  return 1 === e ? "987-06e74253d3d6fdb0.js" : 2 === e ? "925-00c5e93da9c24f8c.js" : "" + e + "." + {
    3: "71a95145b3123fb2",
    4: "1010f2d05ea9d916",
    5: "fa155d24f1aa38e7"
  }[e] + ".js";
}
```
There might even be switch cases however I have not encountered them in the wild, but added in just in case:
```js
e => {
  switch (e) {
    case 1: return "5bc7d56beddd675ee02a.js";
    case 2: return "bee97256afba34de130e.js";
    case 3: return "c2ce9ce9700e2a0ab724.js";
  }
}
```

### Chunk ID Type Precaution
Notice how the matchings inside `if` and `ternary` are strict comparisons `===`. This means the chunk ID must be the same type as the matcher condition, and a type mismatch would cause the loader to miss chunk files. Chunk IDs will become numbers as strings when you use `optimization: {minimize: false}`:
```js
chunkId => {
  return chunkId + "." + {
    "400": "522c174281df6867160f",
    "411": "defd75d4e0e0082175d2",
    "826": "6a4d614c9886ec0ba82a"
  }[chunkId] + ".js";
}
```
So it is best to preserve the ID type (number or string) when extracting the chunk IDs.

### Extraction
To extract the chunk IDs you walk the loader tree and check a few different cases:
- For object expressions: go through each property extract all the keys
- For `if`/ternary: go through each comparison and extract the literals (it can be `e === 1` or `1 === e`)
- For `switch`: go through each `case` and extract the literals

Note that multiple matchers can appear in the same loader (e.g., match using ternary first then use object expression), so make sure you don't stop after finding a matcher. Also if no chunk IDs are extracted, it possibly means that the loader is not actually a loader, but a false positive from loader detection stage.

## Chunk Filename Extraction
With all the chunk IDs ready you can run the loader function for each chunk ID, which will give you the chunk filenames. In most cases you can just run the loader directly, but some loaders will reference a member:
```js
function(e) {
  return o.p + e + "." + {
    0: "36fdb28032f45d0673c2",
    1: "59db5f353838b3001f26",
    2: "d67ce0f1f4b3bd4afdd0"
  }[e] + ".js";
}
```
Without knowing `o.p` the loader function cannot run. Usually the `o.p` is directly assigned to a literal somewhere else in the script (e.g., `o.p = "chunk/"`). This can be fixed by searching the script for all assignments, and set the correct variables before running the loader.

## Webpack Site
A Webpack site can have multiple chunk loaders. This happens when you specify multiple entry points in the Webpack config. Sometimes a chunk loader script will only be loaded after a specific user action, such as navigate to another page, login, clicking a button etc. So it is important that you action all features on a site when searching for chunk loaders.
