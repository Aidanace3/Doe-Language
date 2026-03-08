# TODO: Fix elses, yields, add in-line errors, add IntelliSense

## Phase 1: Parser.cs Improvements

- [x] 1.1 Fix else validation - Add check for else without matching if
- [x] 1.2 Fix else validation - Add check for multiple else in same if-block
- [x] 1.3 Fix yield validation - Add check for yield without point reference
- [x] 1.4 Fix yield validation - Validate yield point exists
- [x] 1.5 Add more in-line errors for common mistakes

## Phase 2: Extension.js Improvements

- [x] 2.1 Add more in-line error detection in grammar checks
- [x] 2.2 Add VSCode completion provider for keywords
- [x] 2.3 Add VSCode completion provider for functions/points
- [x] 2.4 Add hover provider for documentation

## Testing

- [x] 3.1 Test error detection with comprehensive_test.doe
- [x] 3.2 Test IntelliSense with various code patterns

