// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest';

beforeAll(async () => {
    await import('../../src/Dashboard/wwwroot/app.js');
});

afterEach(() => {
    document.body.innerHTML = '';
    document.documentElement.style.cssText = '';
    document.documentElement.removeAttribute('data-bs-theme');
    window.history.replaceState({}, '', '/');
    vi.restoreAllMocks();
});

function createBenchmarkItem(overrides = {}) {
    return {
        commit: {
            author: {
                username: 'martincostello',
            },
            message: 'Improve benchmark stability',
            sha: '0123456789abcdef',
            timestamp: '2026-05-02T08:00:00Z',
            url: 'https://github.com/martincostello/benchmarks-dashboard/commit/0123456789abcdef',
        },
        result: {
            bytesAllocated: null,
            memoryUnit: 'bytes',
            range: null,
            unit: 'ns',
            value: 123.456,
        },
        timestamp: '2026-05-02T08:00:00Z',
        ...overrides,
    };
}

function createDependencies(overrides = {}) {
    return {
        ClipboardConstructor: undefined,
        ClipboardItemCtor: {
            supports: vi.fn(() => false),
        },
        PlotlyRef: {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        },
        bootstrapRef: {
            Tooltip: vi.fn(),
        },
        btoaRef: (value) => Buffer.from(value, 'binary').toString('base64'),
        documentRef: document,
        fetchRef: vi.fn(),
        getComputedStyleRef: window.getComputedStyle.bind(window),
        navigatorRef: {
            clipboard: {
                write: vi.fn(),
                writeText: vi.fn().mockResolvedValue(undefined),
            },
        },
        navigateRef: vi.fn(),
        openRef: vi.fn(),
        requestAnimationFrameRef: vi.fn((callback) => callback()),
        setTimeoutRef: vi.fn((callback) => callback()),
        windowRef: window,
        ...overrides,
    };
}

describe('DashboardApp', () => {
    it('applies the current theme colors to a layout', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const layout = {
            title: {},
            xaxis: {},
            yaxis: {},
            yaxis2: {},
        };

        app.applyThemeToLayout(layout);

        expect(layout).toMatchObject({
            font: {
                color: '#123456',
            },
            paper_bgcolor: '#abcdef',
            plot_bgcolor: '#abcdef',
            title: {
                font: {
                    color: '#123456',
                },
            },
            xaxis: {
                color: '#123456',
            },
            yaxis: {
                color: '#123456',
            },
            yaxis2: {
                color: '#123456',
            },
        });
    });

    it('builds deep links from SVG title links when repository filters are selected', () => {
        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
    `;

        window.location.hash = '#Old Value';

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const target = document.createElement('a');
        target.setAttribute('xlink:href', '#My Benchmark');

        const url = app.createDeepLinkUrl(target);

        expect(url?.pathname).toBe(window.location.pathname);
        expect(url?.hash).toBe('#My%20Benchmark');
        expect(url?.searchParams.get('repo')).toBe('martincostello/benchmarks-dashboard');
        expect(url?.searchParams.get('branch')).toBe('main');
    });

    it('creates chart definitions with a memory series and error bars', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = '<div id="suite-name"><div id="chart"></div></div>';

        Object.defineProperty(document.documentElement, 'clientWidth', {
            configurable: true,
            value: 1280,
        });

        const dataset = [
            createBenchmarkItem({
                result: {
                    bytesAllocated: 0,
                    memoryUnit: 'KB',
                    range: '± 0.42',
                    unit: 'ms',
                    value: 12.34,
                },
            }),
            createBenchmarkItem({
                commit: {
                    author: {
                        username: 'martin_costello',
                    },
                    message: 'Add memory column',
                    sha: 'fedcba9876543210',
                    timestamp: '2026-05-03T08:00:00Z',
                    url: 'https://github.com/martincostello/benchmarks-dashboard/commit/fedcba9876543210',
                },
                result: {
                    bytesAllocated: 0,
                    memoryUnit: 'KB',
                    range: '± 0.84',
                    unit: 'ms',
                    value: 23.45,
                },
            }),
        ];

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const definition = app.createChartDefinition('chart', {
            colors: {
                memory: '#e34c26',
                time: '#178600',
            },
            dataset,
            errorBars: true,
            imageFormat: 'png',
            name: 'My Benchmark',
        });

        expect(definition.data).toHaveLength(2);
        expect(definition.data[0].error_y).toEqual({
            array: [0.42, 0.84],
            type: 'data',
        });
        expect(definition.data[1]).toMatchObject({
            name: 'Memory',
            text: ['0.00KB', '0.00KB'],
            yaxis: 'y2',
        });
        expect(definition.layout.title.text).toContain('href="#suite-name"');
        expect(definition.layout.yaxis.title.text).toBe('t (ms)');
        expect(definition.layout.yaxis2).toMatchObject({
            maxallowed: 1,
            tickformat: '.0f',
            tickmode: 'linear',
            title: {
                text: 'KB',
            },
        });
    });

    it('HTML-encodes untrusted strings in chart definitions', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = '<div id="suite-name"><div id="chart"></div></div>';

        Object.defineProperty(document.documentElement, 'clientWidth', {
            configurable: true,
            value: 1280,
        });

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const definition = app.createChartDefinition('chart', {
            colors: {
                memory: '#e34c26',
                time: '#178600',
            },
            dataset: [
                createBenchmarkItem({
                    commit: {
                        author: {
                            username: 'user<script>',
                        },
                        message: '<img src=x onerror=alert(1)>\nQuoted "message"',
                        sha: '0123456789abcdef',
                        timestamp: '2026-05-02T08:00:00Z<script>',
                        url: 'https://github.com/martincostello/benchmarks-dashboard/commit/0123456789abcdef',
                    },
                }),
            ],
            errorBars: false,
            imageFormat: 'png',
            name: '<script>alert(1)</script>',
        });

        expect(definition.layout.title.text).toContain('&lt;script&gt;alert(1)&lt;/script&gt;');
        expect(definition.layout.title.text).not.toContain('<script>');
        expect(definition.data[0].customdata[0]).toContain('&lt;img src=x onerror=alert(1)&gt;');
        expect(definition.data[0].customdata[0]).toContain('Quoted &quot;message&quot;');
        expect(definition.data[0].customdata[0]).toContain('2026-05-02T08:00:00Z&lt;script&gt; authored by @user&lt;script&gt;');
    });

    it('HTML-encodes the chart anchor id in chart definitions', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = '<div id=\'suite"&lt;unsafe&gt;\'><div id="chart"></div></div>';

        Object.defineProperty(document.documentElement, 'clientWidth', {
            configurable: true,
            value: 1280,
        });

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const definition = app.createChartDefinition('chart', {
            colors: {
                memory: '#e34c26',
                time: '#178600',
            },
            dataset: [createBenchmarkItem()],
            errorBars: false,
            imageFormat: 'png',
            name: 'My Benchmark',
        });

        expect(definition.layout.title.text).toContain('href="#suite%22%3Cunsafe%3E"');
        expect(definition.layout.title.text).not.toContain('href="#suite"&lt;unsafe&gt;"');
    });

    it('uses the anchor element for deep links when a child node is clicked', () => {
        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <a class="benchmark-anchor" href="https://benchmarks.martincostello.com/#suite-name">
        <span class="child"></span>
        <span class="fade"></span>
      </a>
    `;

        const navigatorRef = {
            clipboard: {
                write: vi.fn(),
                writeText: vi.fn(),
            },
        };

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                navigatorRef,
                setTimeoutRef: vi.fn(),
            })
        );

        app.configureDeepLinks();

        const child = document.querySelector('.child');
        child.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(navigatorRef.clipboard.writeText).toHaveBeenCalledWith(
            'https://benchmarks.martincostello.com/?repo=martincostello%2Fbenchmarks-dashboard&branch=main#suite-name'
        );
    });

    it('only shows deep link confirmation after a successful clipboard write', async () => {
        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <a class="benchmark-anchor" href="https://benchmarks.martincostello.com/#suite-name">
        <span class="fade"></span>
      </a>
    `;

        const navigatorRef = {
            clipboard: {
                write: vi.fn(),
                writeText: vi.fn().mockResolvedValue(undefined),
            },
        };

        const setTimeoutRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                navigatorRef,
                setTimeoutRef,
            })
        );

        app.configureDeepLinks();

        const anchor = document.querySelector('.benchmark-anchor');
        const icon = document.querySelector('.fade');

        anchor.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await Promise.resolve();

        expect(navigatorRef.clipboard.writeText).toHaveBeenCalledOnce();
        expect(icon.classList.contains('show')).toBe(true);
        expect(icon.title).toBe('URL copied to clipboard');
        expect(setTimeoutRef).toHaveBeenCalledOnce();
    });

    it('does not show deep link confirmation when clipboard write fails', async () => {
        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <a class="benchmark-anchor" href="https://benchmarks.martincostello.com/#suite-name">
        <span class="fade"></span>
      </a>
    `;

        const navigatorRef = {
            clipboard: {
                write: vi.fn(),
                writeText: vi.fn().mockRejectedValue(new Error('copy failed')),
            },
        };

        const setTimeoutRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                navigatorRef,
                setTimeoutRef,
            })
        );

        app.configureDeepLinks();

        const anchor = document.querySelector('.benchmark-anchor');
        const icon = document.querySelector('.fade');

        anchor.dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await Promise.resolve();

        expect(navigatorRef.clipboard.writeText).toHaveBeenCalledOnce();
        expect(icon.classList.contains('show')).toBe(false);
        expect(icon.title).toBe('');
        expect(setTimeoutRef).not.toHaveBeenCalled();
    });

    it('replaces existing repo and branch query parameters in deep links', () => {
        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-01" value="2026-05-01" />
      <input id="endDate" max="2026-05-31" value="2026-05-31" />
    `;

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const target = document.createElement('a');
        target.href = 'https://benchmarks.martincostello.com/?repo=old/repo&branch=dev#suite-name';

        const url = app.createDeepLinkUrl(target);

        expect(url?.toString()).toBe(
            'https://benchmarks.martincostello.com/?repo=martincostello%2Fbenchmarks-dashboard&branch=main#suite-name'
        );
        expect(url?.searchParams.getAll('repo')).toEqual(['martincostello/benchmarks-dashboard']);
        expect(url?.searchParams.getAll('branch')).toEqual(['main']);
        expect(url?.searchParams.get('startDate')).toBeNull();
        expect(url?.searchParams.get('endDate')).toBeNull();
    });

    it('persists non-default date filters in deep links', () => {
        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-01" value="2026-05-02" />
      <input id="endDate" max="2026-05-31" value="2026-05-30" />
    `;

        const app = window.DashboardApp.createDashboardApp(createDependencies());
        const target = document.createElement('a');
        target.href = 'https://benchmarks.martincostello.com/#suite-name';

        const url = app.createDeepLinkUrl(target);

        expect(url?.toString()).toBe(
            'https://benchmarks.martincostello.com/?repo=martincostello%2Fbenchmarks-dashboard&branch=main&startDate=2026-05-02&endDate=2026-05-30#suite-name'
        );
        expect(url?.searchParams.get('startDate')).toBe('2026-05-02');
        expect(url?.searchParams.get('endDate')).toBe('2026-05-30');
    });

    it('renders charts and sanitizes downloaded image filenames', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const openRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                ClipboardItemCtor: {
                    supports: vi.fn(() => false),
                },
                PlotlyRef: plotly,
                openRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [createBenchmarkItem()],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark: #1/2',
            })
        );

        expect(plotly.newPlot).toHaveBeenCalledTimes(1);
        expect(document.getElementById('chart-copy')).toMatchObject({
            disabled: true,
        });

        handlers.get('plotly_click')({
            points: [
                {
                    pointIndex: 0,
                },
            ],
        });

        expect(openRef).toHaveBeenCalledWith('https://github.com/martincostello/benchmarks-dashboard/commit/0123456789abcdef', '_blank');

        document.getElementById('chart-download').click();

        expect(plotly.downloadImage).toHaveBeenCalledWith(chart, {
            filename: 'My_Benchmark___1_2.png',
            format: 'png',
        });
    });

    it('applies a dragged chart selection as a shared date filter', () => {
        window.history.replaceState({}, '', '/');

        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-01" value="2026-05-01" />
      <input id="endDate" max="2026-05-31" value="2026-05-31" />
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const navigateRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                navigateRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [
                    createBenchmarkItem({ timestamp: '2026-05-02T08:00:00Z' }),
                    createBenchmarkItem({
                        commit: {
                            author: {
                                username: 'martin_costello',
                            },
                            message: 'Add more data',
                            sha: 'fedcba9876543210',
                            timestamp: '2026-05-12T08:00:00Z',
                            url: 'https://github.com/martincostello/benchmarks-dashboard/commit/fedcba9876543210',
                        },
                        timestamp: '2026-05-12T08:00:00Z',
                    }),
                ],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_selected')({
            points: [
                {
                    pointIndex: 1,
                },
                {
                    pointIndex: 0,
                },
                {
                    pointIndex: 1,
                },
            ],
        });

        expect(navigateRef).toHaveBeenCalledWith(
            `${window.location.origin}/?repo=martincostello%2Fbenchmarks-dashboard&branch=main&startDate=2026-05-02&endDate=2026-05-12#suite-name`
        );
    });

    it('uses the configured in-app date filter callback for dragged chart selections', async () => {
        window.history.replaceState({}, '', '/');

        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-01" value="2026-05-01" />
      <input id="endDate" max="2026-05-31" value="2026-05-31" />
      <div class="d-none" id="date-range-loader"></div>
      <div id="benchmarks">
        <div id="suite-name">
          <div id="chart"></div>
        </div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const navigateRef = vi.fn();
        const navigationRef = {
            invokeMethodAsync: vi.fn().mockResolvedValue(undefined),
        };
        const scheduledCallbacks = [];
        const setTimeoutRef = vi.fn((callback) => {
            scheduledCallbacks.push(callback);
        });

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                navigateRef,
                setTimeoutRef,
            })
        );

        app.configureDateFilterNavigation(navigationRef);

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [
                    createBenchmarkItem({ timestamp: '2026-05-02T08:00:00Z' }),
                    createBenchmarkItem({ timestamp: '2026-05-12T08:00:00Z' }),
                ],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_selected')({
            points: [
                {
                    pointIndex: 1,
                },
                {
                    pointIndex: 0,
                },
            ],
        });

        expect(document.getElementById('date-range-loader')?.classList.contains('d-none')).toBe(false);
        expect(document.getElementById('benchmarks')?.classList.contains('d-none')).toBe(true);
        expect(setTimeoutRef).toHaveBeenCalledOnce();
        expect(navigationRef.invokeMethodAsync).not.toHaveBeenCalled();
        expect(navigateRef).not.toHaveBeenCalled();

        scheduledCallbacks[0]();

        await Promise.resolve();

        expect(navigationRef.invokeMethodAsync).toHaveBeenCalledWith(
            'ApplyDateRangeFromChartAsync',
            '2026-05-02',
            '2026-05-12',
            '#suite-name'
        );
        expect(navigateRef).not.toHaveBeenCalled();
    });

    it('omits default date filters when a dragged chart selection matches the configured bounds', () => {
        window.history.replaceState({}, '', '/');

        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-02" value="2026-05-02" />
      <input id="endDate" max="2026-05-12" value="2026-05-12" />
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const navigateRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                navigateRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [
                    createBenchmarkItem({ timestamp: '2026-05-02T08:00:00Z' }),
                    createBenchmarkItem({ timestamp: '2026-05-12T08:00:00Z' }),
                ],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_selected')({
            points: [
                {
                    pointIndex: 0,
                },
                {
                    pointIndex: 1,
                },
            ],
        });

        expect(navigateRef).toHaveBeenCalledWith(
            `${window.location.origin}/?repo=martincostello%2Fbenchmarks-dashboard&branch=main#suite-name`
        );
    });

    it('persists only non-default dragged date filters when one bound matches the configured range', () => {
        window.history.replaceState({}, '', '/');

        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-02" value="2026-05-02" />
      <input id="endDate" max="2026-05-12" value="2026-05-12" />
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const navigateRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                navigateRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [
                    createBenchmarkItem({ timestamp: '2026-05-02T08:00:00Z' }),
                    createBenchmarkItem({ timestamp: '2026-05-10T08:00:00Z' }),
                ],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_selected')({
            points: [
                {
                    pointIndex: 0,
                },
                {
                    pointIndex: 1,
                },
            ],
        });

        expect(navigateRef).toHaveBeenCalledWith(
            `${window.location.origin}/?repo=martincostello%2Fbenchmarks-dashboard&branch=main&endDate=2026-05-10#suite-name`
        );
    });

    it('rejects dragged chart selections outside the configured date bounds even when inputs are empty', () => {
        window.history.replaceState({}, '', '/');

        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-03" value="" />
      <input id="endDate" max="2026-05-31" value="" />
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const navigateRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                navigateRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [createBenchmarkItem({ timestamp: '2026-05-02T08:00:00Z' })],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_selected')({
            points: [
                {
                    pointIndex: 0,
                },
            ],
        });

        expect(navigateRef).not.toHaveBeenCalled();
    });

    it('clears the drag selection when no benchmark points are selected', () => {
        window.history.replaceState({}, '', '/');

        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <input id="repository" value="martincostello/benchmarks-dashboard" />
      <input id="branch" value="main" />
      <input id="startDate" min="2026-05-01" value="2026-05-01" />
      <input id="endDate" max="2026-05-31" value="2026-05-31" />
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const navigateRef = vi.fn();

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                navigateRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [
                    createBenchmarkItem({ timestamp: '2026-05-02T08:00:00Z' }),
                    createBenchmarkItem({ timestamp: '2026-05-12T08:00:00Z' }),
                ],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_selected')({
            points: [],
        });

        expect(navigateRef).not.toHaveBeenCalled();
        expect(plotly.relayout).toHaveBeenCalledWith(chart, {
            selections: [],
        });
    });

    it('uses the injected window for default open behavior', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        const handlers = new Map();
        chart.on = vi.fn((eventName, callback) => {
            handlers.set(eventName, callback);
        });

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const windowRef = {
            open: vi.fn(),
        };

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                PlotlyRef: plotly,
                openRef: undefined,
                windowRef,
            })
        );

        app.renderChart(
            'chart',
            JSON.stringify({
                colors: {
                    memory: '#e34c26',
                    time: '#178600',
                },
                dataset: [createBenchmarkItem()],
                errorBars: false,
                imageFormat: 'png',
                name: 'My Benchmark',
            })
        );

        handlers.get('plotly_click')({
            points: [
                {
                    pointIndex: 0,
                },
            ],
        });

        expect(windowRef.open).toHaveBeenCalledWith(
            'https://github.com/martincostello/benchmarks-dashboard/commit/0123456789abcdef',
            '_blank'
        );
    });

    it('disables chart clipboard when ClipboardItemCtor is not available', () => {
        document.documentElement.style.setProperty('--bs-body-color', '#123456');
        document.documentElement.style.setProperty('--bs-body-bg', '#abcdef');
        document.documentElement.style.setProperty('--plot-hover-color', '#111111');
        document.documentElement.style.setProperty('--plot-hover-background-color', '#222222');
        document.documentElement.style.setProperty('--bs-font-sans-serif', 'Inter');

        document.body.innerHTML = `
      <div id="suite-name">
        <div id="chart"></div>
      </div>
      <button id="chart-copy"></button>
      <button id="chart-download"></button>
      <div class="nsewdrag"></div>
    `;

        const chart = document.getElementById('chart');
        chart.on = vi.fn();

        const plotly = {
            downloadImage: vi.fn(),
            newPlot: vi.fn(),
            relayout: vi.fn(),
            toImage: vi.fn(),
        };

        const app = window.DashboardApp.createDashboardApp(
            createDependencies({
                ClipboardItemCtor: undefined,
                PlotlyRef: plotly,
            })
        );

        expect(() =>
            app.renderChart(
                'chart',
                JSON.stringify({
                    colors: {
                        memory: '#e34c26',
                        time: '#178600',
                    },
                    dataset: [createBenchmarkItem()],
                    errorBars: false,
                    imageFormat: 'png',
                    name: 'My Benchmark',
                })
            )
        ).not.toThrow();

        expect(document.getElementById('chart-copy')).toMatchObject({
            disabled: true,
        });
    });
});
