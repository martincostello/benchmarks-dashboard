'use strict';

window.scrollToActiveChart = () => {
  if (window.location.hash) {
    const focus = window.location.hash.substring(1);
    let element = document.getElementById(focus);
    if (!element) {
      element = document.getElementById(decodeURIComponent(focus));
    }
    if (element) {
      element.scrollIntoView(false);
    }
  }
};

window.configureClipboard = () => {
  if ('ClipboardJS' in window) {
    const selector = '.copy-button';
    const clipboard = window['ClipboardJS'];
    new clipboard(selector);
    document.querySelectorAll(selector).forEach((element) => {
      element.addEventListener('click', (event) => {
        event.preventDefault();
      });
    });
  }
};

const copyDeepLink = (element) => {
  const repo = document.getElementById('repository').value;
  const branch = document.getElementById('branch').value;

  if (!branch || !repo) {
    return;
  }

  const url = new URL(element.target.href);

  url.searchParams.append('repo', repo);
  url.searchParams.append('branch', branch);

  navigator.clipboard.writeText(url.href);

  const icon = element.target.querySelector('.fade');
  if (icon) {
    icon.title = 'URL copied to clipboard';
    icon.classList.add('show');
    setTimeout(() => {
      icon.classList.remove('show');
    }, 3000);
  }
};

window.configureDeepLinks = () => {
  const anchors = [...document.querySelectorAll('.benchmark-set-anchor')];
  for (const anchor of anchors) {
    anchor.removeEventListener('click', copyDeepLink);
    anchor.addEventListener('click', copyDeepLink);
  }
};

window.configureToolTips = () => {
  const tooltips = [...document.querySelectorAll('[data-bs-toggle="tooltip"]')];
  tooltips.map((element) => new bootstrap.Tooltip(element));
};

window.configureDataDownload = (json, fileName) => {
  const element = document.getElementById('download-json');
  if (element) {
    element.onclick = () => {
      // See https://developer.mozilla.org/docs/Glossary/Base64#the_unicode_problem
      const encoder = new TextEncoder();
      const bytes = encoder.encode(json);
      const binaryString = Array.from(bytes, (byte) => String.fromCodePoint(byte)).join('');
      const jsonAsBase64 = btoa(binaryString);
      const dataUrl = `data:text/json;base64,${jsonAsBase64}`;
      const link = document.createElement('a');
      link.href = dataUrl;
      link.download = fileName;
      link.click();
    };
  }
};

window.renderChart = (chartId, configString) => {
  const config = JSON.parse(configString);
  const { dataset } = config;

  const isDesktop = document.documentElement.clientWidth > 576;

  const memoryAxis = 'y2';
  const memoryColor = config.colors.memory;
  const timeAxis = 'y';
  const timeColor = config.colors.time;

  const mode = 'lines+markers';
  const shape = 'spline';
  const type = 'scatter';

  const rounding = 2;
  const precision = rounding + 1;

  const layout = {
    font: {
      size: isDesktop ? 10 : 8,
    },
    legend: {
      orientation: 'h',
      y: isDesktop ? -0.15 : -0.1,
    },
    title: {
      text: config.name,
    },
    xaxis: {
      fixedrange: true,
      tickangle: -30,
      title: {
        text: 'Commit',
      },
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

  if (!isDesktop) {
    layout.margin = {
      l: 30,
      r: 30,
      t: 30,
      b: 10,
    };
  }

  const seriesX = dataset.map((p) => p.commit.sha.slice(0, 7));

  const mapTimeText = (item) => {
    const { range, unit } = item.result;
    let label = `${item.result.value.toFixed(rounding)}${unit}`;
    if (range) {
      const prefix = range.slice(0, 2);
      const rangeValue = parseFloat(range.slice(2)).toPrecision(precision);
      label += ` (${prefix}${rangeValue})`;
    }
    return label;
  };

  const newline = `<br>`;
  const customdata = dataset.map((item) => {
    const message = item.commit.message
      .split('\n')
      .slice(0, 20)
      .map((p) => p.length > 70 ? p.slice(0, 70) + '...' : p)
      .join(newline);

    return message +
      newline +
      newline +
      `${item.commit.timestamp} authored by @${item.commit.author.username}` +
      newline;
  });

  const hoverlabel = {
    align: 'left',
    bordercolor: 'black',
    font: {
      color: getComputedStyle(document.documentElement).getPropertyValue('--plot-hover-color'),
      family: getComputedStyle(document.documentElement).getPropertyValue('--bs-font-sans-serif'),
    }
  };

  const hovertemplate =
    "<b>%{text}</b>" +
    newline +
    newline +
    "%{x}" +
    newline +
    newline +
    "%{customdata}" +
    "<extra></extra>";

  const time = {
    connectgaps: true,
    customdata,
    fill: 'tozeroy',
    hoverlabel,
    hovertemplate,
    line: {
      color: timeColor,
      shape,
    },
    marker: {
      color: timeColor,
    },
    mode,
    name: 'Time',
    text: dataset.map(mapTimeText),
    type,
    x: seriesX,
    y: dataset.map((p) => p.result.value),
    yaxis: timeAxis,
  };

  if (config.errorBars === true) {
    time.error_y = {
      array: dataset.map((p) => parseFloat(p.result.range.slice(2))),
      type: 'data',
    };
  }

  const data = [time];

  const hasMemory = dataset.some((p) => p.result.bytesAllocated !== null);
  if (hasMemory) {
    const defaultMemoryUnit = 'bytes';
    const memoryUnit = dataset.find((p) => p.result.bytesAllocated !== null)?.result.memoryUnit ?? defaultMemoryUnit;
    const memoryTextSuffix = memoryUnit === defaultMemoryUnit ? ` ${memoryUnit}` : memoryUnit;
    const places = memoryUnit === defaultMemoryUnit ? 0 : rounding;

    const mapMemoryText = (item) => {
      if (item.result.bytesAllocated !== null) {
        return `${item.result.bytesAllocated.toFixed(places)}${memoryTextSuffix}`;
      }
      return undefined;
    };

    const memory = {
      connectgaps: true,
      customdata,
      hoverlabel,
      hovertemplate,
      line: {
        color: memoryColor,
        shape,
      },
      marker: {
        color: memoryColor,
        symbol: 'triangle-up',
      },
      mode,
      name: 'Memory',
      text: dataset.map(mapMemoryText),
      type,
      x: seriesX,
      y: dataset.map((p) => p.result.bytesAllocated),
      yaxis: memoryAxis,
    };

    data.push(memory);

    layout.yaxis2 = {
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

    const allZero = dataset.every((p) => p.result.bytesAllocated === 0);
    if (allZero) {
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

  Plotly.newPlot(chartId, data, layout, plotConfig);

  const chart = document.getElementById(chartId);
  chart.on('plotly_click', (data) => {
    const { pointIndex } = data.points[0];
    const url = dataset[pointIndex].commit.url;
    window.open(url, '_blank');
  });

  // Borrowed from .NET Aspire: https://github.com/dotnet/aspire/blob/84bd9f75ab096a1cf9b8ea8e69914445aaf23d8c/src/Aspire.Dashboard/wwwroot/js/app-metrics.js#L89-L118
  const dragLayer = document.getElementsByClassName('nsewdrag')[0];
  dragLayer.style.cursor = 'default';
  chart.on('plotly_hover', () => {
    dragLayer.style.cursor = 'pointer';
  });
  chart.on('plotly_unhover', () => {
    dragLayer.style.cursor = 'default';
  });

  const saveButton = document.getElementById(`${chartId}-download`);
  saveButton.addEventListener('click', async () => {
    const format = config.imageFormat;
    let fileName = config.name;
    for (const toReplace of [' ', '#', ':', ';', '/', '\\']) {
      fileName = fileName.replace(toReplace, '_');
    }
    fileName = `${fileName}.${format}`;
    const dataUrl = await Plotly.toImage(chart, {
      format,
    });
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    link.click();
  });
};
