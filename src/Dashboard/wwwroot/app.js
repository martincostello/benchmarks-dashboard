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

window.renderChart = (canvasId, configString) => {
  const config = JSON.parse(configString);

  const memoryAxis = 'y2';
  const memoryColor = config.colors.memory;
  const timeAxis = 'y';
  const timeColor = config.colors.time;

  const { dataset } = config;

  const data = {
    labels: dataset.map(d => d.commit.sha.slice(0, 7)),
    datasets: [
      {
        label: 'Time',
        data: dataset.map(d => d.result.value),
        borderColor: timeColor,
        backgroundColor: `${timeColor}60`,
        fill: true,
        tension: 0.4,
        yAxisID: timeAxis,
      },
    ],
  };

  const options = {
    scales: {
      x: {
        title: {
          display: true,
          text: 'Commit',
        },
      },
      y: {
        beginAtZero: true,
        title: {
          display: true,
          text: dataset.length > 0 ? `t (${dataset[0].result.unit})` : '',
        },
      },
    },
    onClick: (_, elements) => {
      if (elements.length > 0) {
        const { index } = elements[0];
        const url = dataset[index].commit.url;
        window.open(url, '_blank');
      }
    },
    plugins: {
      title: {
        display: true,
        text: config.name,
      },
      tooltip: {
        callbacks: {
          afterTitle: items => {
            const data = dataset[items[0].dataIndex];
            return `\n${data.commit.message}\n\n${data.commit.timestamp} authored by @${data.commit.author.username}\n`;
          },
          label: context => {
            const item = dataset[context.dataIndex];
            const memory = context.datasetIndex === 1;
            let label;
            if (memory && item && item.result.bytesAllocated !== null) {
              label = item.result.bytesAllocated.toString();
              label += item.result.memoryUnit ?? ' bytes';
            } else {
              label = item.result.value.toString();
              const { range, unit } = item.result;
              label += unit;
              if (range) {
                label += ` (${range})`;
              }
            }
            return label;
          },
          afterLabel: context => {
            const { extra } = dataset[context.dataIndex].result;
            return extra ? `\n${extra}` : '';
          }
        },
      },
    },
  };

  const hasMemory = dataset.some(d => d.result.bytesAllocated !== null);
  if (hasMemory) {
    const memoryUnit = dataset.find(d => d.result.bytesAllocated !== null)?.result.memoryUnit ?? 'bytes';
    data.datasets.push({
      label: 'Memory',
      data: dataset.map(d => d.result.bytesAllocated),
      borderColor: memoryColor,
      backgroundColor: `${memoryColor}60`,
      fill: false,
      pointStyle: 'triangle',
      tension: 0.4,
      yAxisID: memoryAxis,
    });
    options.scales[memoryAxis] = {
      beginAtZero: true,
      position: 'right',
      title: {
        display: hasMemory,
        text: memoryUnit,
      },
    };

    const allZero = dataset.every(d => d.result.bytesAllocated === 0);
    if (allZero) {
      options.scales[memoryAxis].ticks = {
        precision: 0,
      };
    }
  }

  const previous = Chart.getChart(canvasId);;
  if (previous) {
    previous.destroy();
  }

  new Chart(document.getElementById(canvasId), {
    type: 'line',
    data,
    options,
  });
};
