// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

'use strict';

const defaultOptions = Object.freeze({
    maxThemeRefreshDepth: 128,
    mobileBreakpoint: 576,
    rounding: 2,
});

function createDashboardApp(dependencies = {}) {
    const {
        windowRef = globalThis.window,
        documentRef = globalThis.document,
        navigatorRef = globalThis.navigator,
        PlotlyRef = globalThis.Plotly,
        bootstrapRef = globalThis.bootstrap,
        ClipboardConstructor = globalThis.ClipboardJS,
        getComputedStyleRef = typeof windowRef?.getComputedStyle === 'function'
            ? windowRef.getComputedStyle.bind(windowRef)
            : typeof globalThis.getComputedStyle === 'function'
              ? globalThis.getComputedStyle.bind(globalThis)
              : undefined,
        requestAnimationFrameRef = typeof windowRef?.requestAnimationFrame === 'function'
            ? windowRef.requestAnimationFrame.bind(windowRef)
            : typeof globalThis.requestAnimationFrame === 'function'
              ? globalThis.requestAnimationFrame.bind(globalThis)
              : undefined,
        setTimeoutRef = typeof windowRef?.setTimeout === 'function'
            ? windowRef.setTimeout.bind(windowRef)
            : typeof globalThis.setTimeout === 'function'
              ? globalThis.setTimeout.bind(globalThis)
              : undefined,
        fetchRef = typeof globalThis.fetch === 'function' ? globalThis.fetch.bind(globalThis) : undefined,
        URLCtor = globalThis.URL,
        TextEncoderCtor = globalThis.TextEncoder,
        btoaRef = typeof globalThis.btoa === 'function' ? globalThis.btoa.bind(globalThis) : undefined,
        ClipboardItemCtor = globalThis.ClipboardItem,
        navigateRef = typeof windowRef?.location?.assign === 'function' ? windowRef.location.assign.bind(windowRef.location) : undefined,
        openRef = typeof windowRef?.open === 'function' ? windowRef.open.bind(windowRef) : undefined,
        options = {},
    } = dependencies;

    const settings = {
        ...defaultOptions,
        ...options,
    };

    const newline = '<br>';
    const htmlEntityMap = Object.freeze({
        '&': '&amp;',
        '"': '&quot;',
        "'": '&#39;',
        '<': '&lt;',
        '>': '&gt;',
    });
    const htmlDecodeMap = Object.freeze({
        '&amp;': '&',
        '&quot;': '"',
        '&#39;': "'",
        '&lt;': '<',
        '&gt;': '>',
    });

    let dateFilterNavigationRef;

    const htmlDecode = (value) => String(value).replaceAll(/&(amp|quot|#39|lt|gt);/g, (match) => htmlDecodeMap[match]);
    const htmlEncode = (value) => String(value).replaceAll(/[&"'<>]/g, (match) => htmlEntityMap[match]);
    const normalizeHtml = (value) => htmlDecode(value);
    const readInputValue = (id) => documentRef.getElementById(id)?.value;
    const setDateRangeRefreshing = (isRefreshing) => {
        const loader = documentRef.getElementById('date-range-loader');
        const benchmarks = documentRef.getElementById('benchmarks');

        loader?.classList.toggle('d-none', !isRefreshing);
        benchmarks?.classList.toggle('d-none', isRefreshing);
    };

    const getThemeStyles = () => {
        const root = documentRef.documentElement;
        const styles = getComputedStyleRef(root);
        const fontColor = styles.getPropertyValue('--bs-body-color').trim();
        const hoverColor = styles.getPropertyValue('--plot-hover-color').trim();
        const hoverBg = styles.getPropertyValue('--plot-hover-background-color').trim();
        const bgColor = styles.getPropertyValue('--bs-body-bg').trim();

        return { fontColor, bgColor, hoverColor, hoverBg };
    };

    const applyThemeToLayout = (layout) => {
        const { fontColor, bgColor } = getThemeStyles();
        layout.font = layout.font || {};
        layout.title = layout.title || {};
        layout.title.font = layout.title.font || {};
        layout.xaxis = layout.xaxis || {};
        layout.yaxis = layout.yaxis || {};

        layout.font.color = fontColor;
        layout.paper_bgcolor = bgColor;
        layout.plot_bgcolor = bgColor;
        layout.title.font.color = fontColor;
        layout.xaxis.color = fontColor;
        layout.yaxis.color = fontColor;

        if (layout.yaxis2) {
            layout.yaxis2.color = fontColor;
        }
    };

    const getHoverLabel = () => {
        const { hoverColor } = getThemeStyles();

        return {
            align: 'left',
            bordercolor: 'black',
            font: {
                color: hoverColor,
                family: getComputedStyleRef(documentRef.documentElement).getPropertyValue('--bs-font-sans-serif'),
            },
        };
    };

    const mapTimeText = (item) => {
        const rounding = settings.rounding;
        const precision = rounding + 1;
        const { range, unit } = item.result;
        let label = item.result.value === 'NaN' ? NaN : `${item.result.value.toFixed(rounding)}${unit}`;

        if (range) {
            const prefix = range.slice(0, 2);
            const rangeValue = parseFloat(range.slice(2)).toPrecision(precision);
            label += ` (${prefix}${rangeValue})`;
        }

        return label;
    };

    const createCustomData = (dataset) =>
        dataset.map((item) => {
            const message = item.commit.message
                .split('\n')
                .slice(0, 20)
                .map((part) => (part.length > 70 ? `${part.slice(0, 70)}...` : part))
                .map((part) => normalizeHtml(part))
                .join(newline);

            const timestamp = normalizeHtml(item.commit.timestamp);
            const author = normalizeHtml(item.commit.author.username);

            return message + newline + newline + `${timestamp} authored by @${author}` + newline;
        });

    const createHoverTemplate = () =>
        '<b>%{text}</b>' + newline + newline + '%{x}' + newline + newline + '%{customdata}' + '<extra></extra>';

    const createTimeSeries = (config, dataset, seriesX, customdata, hoverlabel) => {
        const time = {
            connectgaps: true,
            customdata,
            fill: 'tozeroy',
            hoverlabel,
            hovertemplate: createHoverTemplate(),
            line: {
                color: config.colors.time,
                shape: 'spline',
            },
            marker: {
                color: config.colors.time,
            },
            mode: 'lines+markers',
            name: 'Time',
            text: dataset.map(mapTimeText),
            type: 'scatter',
            x: seriesX,
            y: dataset.map((item) => item.result.value),
            yaxis: 'y',
        };

        if (config.errorBars === true) {
            time.error_y = {
                array: dataset.map((item) => parseFloat(item.result.range?.slice(2) ?? 'NaN')),
                type: 'data',
            };
        }

        return time;
    };

    const createMemorySeries = (config, dataset, seriesX, customdata, hoverlabel) => {
        const defaultMemoryUnit = 'bytes';
        const memoryUnit = dataset.find((item) => item.result.bytesAllocated !== null)?.result.memoryUnit ?? defaultMemoryUnit;
        const memoryTextSuffix = memoryUnit === defaultMemoryUnit ? ` ${memoryUnit}` : memoryUnit;
        const places = memoryUnit === defaultMemoryUnit ? 0 : settings.rounding;

        const memory = {
            connectgaps: true,
            customdata,
            hoverlabel,
            hovertemplate: createHoverTemplate(),
            line: {
                color: config.colors.memory,
                shape: 'spline',
            },
            marker: {
                color: config.colors.memory,
                symbol: 'triangle-up',
            },
            mode: 'lines+markers',
            name: 'Memory',
            text: dataset.map((item) =>
                item.result.bytesAllocated !== null ? `${item.result.bytesAllocated.toFixed(places)}${memoryTextSuffix}` : undefined
            ),
            type: 'scatter',
            x: seriesX,
            y: dataset.map((item) => item.result.bytesAllocated),
            yaxis: 'y2',
        };

        return { memory, memoryUnit };
    };

    const createChartDefinition = (chartId, config) => {
        const { dataset } = config;
        const isDesktop = documentRef.documentElement.clientWidth > settings.mobileBreakpoint;
        const chart = documentRef.getElementById(chartId);
        const parent = chart.parentElement;
        const chartLink = htmlEncode(`#${encodeURIComponent(parent.id)}`);
        const chartTitle = `${htmlEncode(config.name)} <a class="benchmark-anchor text-secondary" href="${chartLink}" target="_self">#</a>`;

        const layout = {
            font: {
                size: isDesktop ? 10 : 8,
            },
            legend: {
                orientation: 'h',
                y: isDesktop ? -0.15 : -0.1,
            },
            title: {
                text: chartTitle,
            },
            xaxis: {
                fixedrange: true,
                tickangle: -30,
                title: {
                    text: 'Commit',
                },
                type: 'category',
            },
            yaxis: {
                fixedrange: true,
                hoverformat: '.2f',
                minallowed: 0,
                rangemode: 'tozero',
                separatethousands: true,
                title: {
                    text: dataset.length > 0 ? `t (${dataset[0].result.unit})` : 't',
                },
            },
        };

        applyThemeToLayout(layout);

        if (!isDesktop) {
            layout.margin = {
                l: 30,
                r: 30,
                t: 30,
                b: 10,
            };
        }

        layout.dragmode = 'select';
        layout.selectdirection = 'h';

        const seriesX = dataset.map((item) => item.commit.sha.slice(0, 7));
        const customdata = createCustomData(dataset);
        const hoverlabel = getHoverLabel();

        const data = [createTimeSeries(config, dataset, seriesX, customdata, hoverlabel)];

        const hasMemory = dataset.some((item) => item.result.bytesAllocated !== null);

        if (hasMemory) {
            const { memory, memoryUnit } = createMemorySeries(config, dataset, seriesX, customdata, hoverlabel);
            data.push(memory);

            layout.yaxis2 = {
                color: layout.font.color,
                fixedrange: true,
                minallowed: 0,
                overlaying: 'y',
                rangemode: 'tozero',
                side: 'right',
                separatethousands: true,
                title: {
                    text: memoryUnit,
                },
            };

            if (dataset.every((item) => item.result.bytesAllocated === 0)) {
                layout.yaxis2.maxallowed = 1;
                layout.yaxis2.tickformat = '.0f';
                layout.yaxis2.tickmode = 'linear';
            }
        }

        const plotConfig = {
            displayModeBar: false,
            responsive: true,
            scrollZoom: false,
        };

        return {
            chart,
            config,
            data,
            dataset,
            layout,
            plotConfig,
        };
    };

    const sanitizeImageFileName = (value) =>
        value
            .replaceAll(' ', '_')
            .replaceAll('#', '_')
            .replaceAll(':', '_')
            .replaceAll(';', '_')
            .replaceAll('/', '_')
            .replaceAll('\\', '_');

    const applyDateRangeFilters = (url, startDate, endDate) => {
        const startDateInput = documentRef.getElementById('startDate');
        const endDateInput = documentRef.getElementById('endDate');

        if (startDate && startDate !== startDateInput?.min) {
            url.searchParams.set('startDate', startDate);
        } else {
            url.searchParams.delete('startDate');
        }

        if (endDate && endDate !== endDateInput?.max) {
            url.searchParams.set('endDate', endDate);
        } else {
            url.searchParams.delete('endDate');
        }
    };

    const applyDashboardFilters = (url, startDate = readInputValue('startDate'), endDate = readInputValue('endDate')) => {
        const repo = readInputValue('repository');
        const branch = readInputValue('branch');

        if (repo) {
            url.searchParams.set('repo', repo);
        }

        if (branch) {
            url.searchParams.set('branch', branch);
        }

        applyDateRangeFilters(url, startDate, endDate);
    };

    const createDeepLinkUrl = (target) => {
        if (!readInputValue('branch') || !readInputValue('repository')) {
            return undefined;
        }

        let href = target.href;

        if (typeof href !== 'string' || href.length < 1) {
            const currentUrl = new URLCtor(windowRef.location.origin);
            currentUrl.pathname = windowRef.location.pathname;
            currentUrl.hash = target.getAttribute('xlink:href');
            href = currentUrl.href;
        }

        const url = new URLCtor(href);
        applyDashboardFilters(url);

        return url;
    };

    const formatDateValue = (value) => {
        if (!value) {
            return undefined;
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return undefined;
        }

        return date.toISOString().slice(0, 10);
    };

    const getSelectedDateRange = (points, dataset) => {
        const uniqueIndexes = [...new Set(points.map((point) => point.pointIndex).filter((pointIndex) => Number.isInteger(pointIndex)))];

        const dates = uniqueIndexes
            .map((pointIndex) => formatDateValue(dataset[pointIndex]?.timestamp))
            .filter((value) => typeof value === 'string')
            .sort();

        if (dates.length < 1) {
            return undefined;
        }

        return {
            endDate: dates[dates.length - 1],
            startDate: dates[0],
        };
    };

    const isValidDateRange = (startDate, endDate) => {
        if (!startDate || !endDate || startDate > endDate) {
            return false;
        }

        const startDateInput = documentRef.getElementById('startDate');
        const endDateInput = documentRef.getElementById('endDate');
        const minimumDate = startDateInput?.min;
        const maximumDate = endDateInput?.max;

        if (minimumDate && startDate < minimumDate) {
            return false;
        }

        if (maximumDate && endDate > maximumDate) {
            return false;
        }

        return true;
    };

    const applyDateFilter = (startDate, endDate, hash) => {
        if (!isValidDateRange(startDate, endDate)) {
            return undefined;
        }

        const url = new URLCtor(windowRef.location.href);
        applyDashboardFilters(url, startDate, endDate);

        const navigateToDateFilter = async () => {
            if (typeof dateFilterNavigationRef?.invokeMethodAsync === 'function') {
                try {
                    await dateFilterNavigationRef.invokeMethodAsync('ApplyDateRangeFromChartAsync', startDate, endDate, hash);
                } catch (error) {
                    console.error('Failed to apply the date filter from chart selection.', error);
                    setDateRangeRefreshing(false);
                }
            } else if (navigateRef) {
                navigateRef(url.toString());
            }
        };

        if (hash) {
            url.hash = hash;
        }

        if (typeof dateFilterNavigationRef?.invokeMethodAsync !== 'function' && !navigateRef) {
            return undefined;
        }

        setDateRangeRefreshing(true);

        if (setTimeoutRef) {
            setTimeoutRef(() => {
                void navigateToDateFilter();
            }, 0);
        } else {
            void navigateToDateFilter();
        }

        return url;
    };

    const configureDateFilterNavigation = (navigationRef) => {
        dateFilterNavigationRef = navigationRef;
    };

    const showCopyConfirmation = (target) => {
        const icon = target.querySelector('.fade');

        if (!icon) {
            return;
        }

        icon.title = 'URL copied to clipboard';
        icon.classList.add('show');

        setTimeoutRef(() => {
            icon.classList.remove('show');
        }, 3000);
    };

    const copyDeepLink = async (event) => {
        const target = event.currentTarget ?? event.target?.closest?.('a');
        const url = target ? createDeepLinkUrl(target) : undefined;

        if (!url) {
            return;
        }

        try {
            await navigatorRef.clipboard.writeText(url.href);
            showCopyConfirmation(target);
        } catch {
            // Ignore
        }

        return false;
    };

    const toggleTheme = () => {
        const newTheme = documentRef.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        const oldBodyColor = getComputedStyleRef(documentRef.body).backgroundColor;
        windowRef._setBenchmarkTheme(newTheme);

        let depth = 0;

        const waitForThemeChange = () => {
            const newBodyColor = getComputedStyleRef(documentRef.body).backgroundColor;

            if (oldBodyColor !== newBodyColor) {
                refreshChartThemes();
            } else if (depth++ < settings.maxThemeRefreshDepth) {
                requestAnimationFrameRef(waitForThemeChange);
            }
        };

        waitForThemeChange();
    };

    const scrollToActiveChart = () => {
        if (!windowRef.location.hash) {
            return;
        }

        const focus = windowRef.location.hash.substring(1);
        let element = documentRef.getElementById(focus);

        if (!element) {
            element = documentRef.getElementById(decodeURIComponent(focus));
        }

        if (element) {
            element.scrollIntoView(false);
        }
    };

    const configureClipboard = () => {
        if (!ClipboardConstructor) {
            return;
        }

        const selector = '.copy-button';
        new ClipboardConstructor(selector);

        documentRef.querySelectorAll(selector).forEach((element) => {
            element.addEventListener('click', (event) => {
                event.preventDefault();
            });
        });
    };

    const configureDeepLinks = () => {
        const anchors = [...documentRef.querySelectorAll('.benchmark-anchor'), ...documentRef.querySelectorAll('text.gtitle > a')];

        for (const anchor of anchors) {
            anchor.removeEventListener('click', copyDeepLink);
            anchor.addEventListener('click', copyDeepLink);
        }
    };

    const configureToolTips = () => {
        const tooltips = [...documentRef.querySelectorAll('[data-bs-toggle="tooltip"]')];
        tooltips.forEach((element) => new bootstrapRef.Tooltip(element));
    };

    const createJsonDataUrl = (json) => {
        // See https://developer.mozilla.org/docs/Glossary/Base64#the_unicode_problem
        const encoder = new TextEncoderCtor();
        const bytes = encoder.encode(json);
        const binaryString = Array.from(bytes, (byte) => String.fromCodePoint(byte)).join('');
        const jsonAsBase64 = btoaRef(binaryString);

        return `data:text/json;base64,${jsonAsBase64}`;
    };

    const configureDataDownload = (json, fileName) => {
        const element = documentRef.getElementById('download-json');

        if (!element) {
            return;
        }

        element.onclick = () => {
            const link = documentRef.createElement('a');
            link.href = createJsonDataUrl(json);
            link.download = fileName;
            link.click();
        };
    };

    const refreshChartThemes = () => {
        if (!PlotlyRef) {
            return;
        }

        const { fontColor, bgColor } = getThemeStyles();

        documentRef.querySelectorAll('.js-plotly-plot').forEach((element) => {
            try {
                PlotlyRef.relayout(element, {
                    'font.color': fontColor,
                    'paper_bgcolor': bgColor,
                    'plot_bgcolor': bgColor,
                    'title.font.color': fontColor,
                    'xaxis.color': fontColor,
                    'yaxis.color': fontColor,
                    'yaxis2.color': fontColor,
                });
            } catch (err) {
                console.error('Failed to relayout chart for theme refresh.', err);
            }
        });
    };

    const configurePlotHover = (chart) => {
        // Borrowed from .NET Aspire: https://github.com/dotnet/aspire/blob/84bd9f75ab096a1cf9b8ea8e69914445aaf23d8c/src/Aspire.Dashboard/wwwroot/js/app-metrics.js#L89-L118
        const dragLayer = documentRef.getElementsByClassName('nsewdrag')[0];

        if (!dragLayer) {
            return;
        }

        dragLayer.style.cursor = 'default';

        chart.on('plotly_hover', () => {
            dragLayer.style.cursor = 'pointer';
        });

        chart.on('plotly_unhover', () => {
            dragLayer.style.cursor = 'default';
        });
    };

    const configureChartClipboard = (chartId, chart, format) => {
        const copyButton = documentRef.getElementById(`${chartId}-copy`);

        if (typeof ClipboardItemCtor?.supports === 'function' && ClipboardItemCtor.supports(`image/${format}`)) {
            copyButton.addEventListener('click', async () => {
                const dataUrl = await PlotlyRef.toImage(chart, {
                    format,
                });
                const data = await fetchRef(dataUrl);
                const blob = await data.blob();

                await navigatorRef.clipboard.write([
                    new ClipboardItemCtor({
                        [blob.type]: blob,
                    }),
                ]);
            });
        } else {
            copyButton.classList.add('disable');
            copyButton.disabled = true;
        }
    };

    const configureChartDownload = (chartId, chart, config) => {
        const saveButton = documentRef.getElementById(`${chartId}-download`);

        saveButton.addEventListener('click', () => {
            PlotlyRef.downloadImage(chart, {
                filename: `${sanitizeImageFileName(config.name)}.${config.imageFormat}`,
                format: config.imageFormat,
            });
        });
    };

    const clearDateSelection = (chart) => {
        PlotlyRef.relayout(chart, {
            selections: [],
        });
    };

    const configureDateSelection = (chartDefinition) => {
        chartDefinition.chart.on('plotly_selected', (event) => {
            const range = getSelectedDateRange(event?.points ?? [], chartDefinition.dataset);

            if (!range || !isValidDateRange(range.startDate, range.endDate)) {
                clearDateSelection(chartDefinition.chart);
                return;
            }

            const chartHash = `#${encodeURIComponent(chartDefinition.chart.parentElement.id)}`;
            applyDateFilter(range.startDate, range.endDate, chartHash);
        });
    };

    const renderChart = (chartId, configString) => {
        const config = JSON.parse(configString);
        const chartDefinition = createChartDefinition(chartId, config);

        PlotlyRef.newPlot(chartId, chartDefinition.data, chartDefinition.layout, chartDefinition.plotConfig);

        chartDefinition.chart.on('plotly_click', (event) => {
            const { pointIndex } = event.points[0];
            openRef(chartDefinition.dataset[pointIndex].commit.url, '_blank');
        });

        configurePlotHover(chartDefinition.chart);
        configureChartClipboard(chartId, chartDefinition.chart, config.imageFormat);
        configureChartDownload(chartId, chartDefinition.chart, config);
        configureDateSelection(chartDefinition);
    };

    const registerGlobals = () => {
        if (!windowRef) {
            return;
        }

        windowRef.toggleTheme = toggleTheme;
        windowRef.scrollToActiveChart = scrollToActiveChart;
        windowRef.configureClipboard = configureClipboard;
        windowRef.configureDateFilterNavigation = configureDateFilterNavigation;
        windowRef.configureDeepLinks = configureDeepLinks;
        windowRef.configureToolTips = configureToolTips;
        windowRef.configureDataDownload = configureDataDownload;
        windowRef.renderChart = renderChart;
        windowRef.refreshChartThemes = refreshChartThemes;
    };

    const api = {
        applyThemeToLayout,
        applyDateFilter,
        configureClipboard,
        configureDataDownload,
        configureDateFilterNavigation,
        configureDeepLinks,
        configureToolTips,
        createChartDefinition,
        createDashboardApp,
        createDeepLinkUrl,
        createJsonDataUrl,
        formatDateValue,
        getThemeStyles,
        getSelectedDateRange,
        isValidDateRange,
        refreshChartThemes,
        registerGlobals,
        renderChart,
        sanitizeImageFileName,
        setDateRangeRefreshing,
        scrollToActiveChart,
        toggleTheme,
    };

    return api;
}

const dashboardApp = createDashboardApp();

if (typeof window !== 'undefined') {
    window.DashboardApp = dashboardApp;
    dashboardApp.registerGlobals();
}
