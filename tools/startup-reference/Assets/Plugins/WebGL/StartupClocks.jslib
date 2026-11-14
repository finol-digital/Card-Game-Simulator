/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mergeInto(LibraryManager.library, {
    // Milliseconds since navigation start, the same origin a browser harness reads.
    CgsPerformanceNow: function () {
        return (typeof performance !== 'undefined' && performance.now) ? performance.now() : -1;
    },

    // Exposes the benchmark result for a browser harness (window.cgsBenchmarkResult).
    CgsSetBenchmarkResult: function (jsonPointer) {
        var json = UTF8ToString(jsonPointer);
        try {
            window.cgsBenchmarkResult = JSON.parse(json);
        } catch (e) {
            window.cgsBenchmarkResult = { outcome: 'failed', reason: 'unparseable result', raw: json };
        }
        console.log('[CgsBenchmark] ' + json);
    }
});
