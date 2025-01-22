# Chunk Search
A prototype project that searches for the chunk loader in Webpack, along with the chunk filenames.

## Note
This is only a prototype for testing chunk loader extraction algorithms. This may or may not work for arbitrary Webpack sites.

## Usage
```
Usage:
  ChunkSearch <url> [options]

Arguments:
  <url>  URL to the site to search for the Webpack chunk loader

Options:
  --file          Specifies the URL is a file path
  --dir           Specifies the URL is a directory path
  --verbose       Specifies verbose output
  --version       Show version information
  -?, -h, --help  Show help and usage information
```

## Example
#### Local JS File
```
dotnet run -c release --project ChunkSearch -- ./Samples/small.js --file
```
Samples JS files can be found under `Samples/`.

#### Local JS Files
```
dotnet run -c release --project ChunkSearch -- ./Samples/ --dir
```

#### Site
```
dotnet run -c release --project ChunkSearch -- https://webpack.js.org/
```

## Generate Webpack Files
Sample webpack project can be found under `Webpack/`. Generate Webpack files by:
```
cd ./Webpack
npm install
npm run build
```
The generated Webpack files will be under `Webpack/dist/`.
