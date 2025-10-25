import React, { useEffect, useState } from 'react';
import Chart from 'chart.js/auto';

// Tailwind CSS classes are assumed to be available
// We will use 'lucide-react' icons for visual polish

const API_BASE = 'https://cashflow-forecasting-fteze2fdauctfzht.canadacentral-01.azurewebsites.net/api';

// --- Icon Imports (Simulated/Assumed availability) ---
const ChartBarIcon = ({ className }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
    <path d="M12 20V10" /><path d="M18 20V4" /><path d="M6 20v-4" />
  </svg>
);
const PlusIcon = ({ className }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
    <path d="M5 12h14" /><path d="M12 5v14" />
  </svg>
);
const XIcon = ({ className }) => (
  <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className}>
    <path d="M18 6 6 18" /><path d="m6 6 12 12" />
  </svg>
);


const App = () => {
  const [records, setRecords] = useState([]);
  const [chartInstance, setChartInstance] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [sampleError, setSampleError] = useState(null);

  // Initialize the chart on component mount
  useEffect(() => {
    initChart();
    fetchData();
    // No dependency array needed for initial data fetch and chart setup
  }, []);

  const initChart = () => {
    const ctx = document.getElementById('dataChart')?.getContext('2d');
    if (!ctx) return;
    const c = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: [],
        datasets: []
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
          },
          title: {
            display: true,
            text: 'Data Values Over Time'
          }
        },
        scales: {
          y: {
            beginAtZero: true
          }
        }
      }
    });
    setChartInstance(c);
  };

  // const updateChart = (data) => {
  //   if (!chartInstance) return;

  //   chartInstance.data = {
  //     labels: data.map(r => r.id.substring(0, 8)),
  //     datasets: [
  //       {
  //         label: 'Value A',
  //         data: data.map(r => r.Type),
  //         backgroundColor: 'rgba(79, 70, 229, 0.7)', // Indigo-500
  //         borderColor: 'rgb(79, 70, 229)',
  //         borderWidth: 1,
  //         borderRadius: 4
  //       },
  //       {
  //         label: 'Value B',
  //         data: data.map(r => r.Amount),
  //         backgroundColor: 'rgba(234, 179, 8, 0.7)', // Amber-500
  //         borderColor: 'rgb(234, 179, 8)',
  //         borderWidth: 1,
  //         borderRadius: 4
  //       }
  //     ]
  //   };
  //   chartInstance.update();
  // };

  async function fetchData() {
    setLoading(true);
    setError(null);
    try {
      // API call simplified: removed Authorization header
      const res = await fetch(`/api/GetTransaction`);
      if (res.ok) {
        const data = await res.json();
        setRecords(data);
        // updateChart(data);
      } else {
        setError(`Failed to fetch data: ${res.statusText} (${res.status})`);
        console.error('Fetch failed', res.status);
      }
    } catch (e) {
      setError('Network or server error during data retrieval.');
      console.error(e);
    } finally {
      setLoading(false);
    }
  }

  const addSample = async () => {
    setSampleError(null);
    const payload = {
      id: crypto.randomUUID(),
      category: `Sample ${new Date().toLocaleTimeString()}`,
      valueA: Math.floor(Math.random() * 200),
      valueB: Math.floor(Math.random() * 200)
    };

    try {
      // API call simplified: removed Authorization header
      const res = await fetch(`/api/AddTransaction`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (res.ok) {
        // Refresh data after successful addition
        fetchData();
      } else {
        // Replaced alert() with state-driven error message
        setSampleError(`Create failed: ${res.statusText} (${res.status}). Check server status.`);
        console.error('Create failed:', res.status);
      }
    } catch (e) {
      setSampleError('Network error while adding sample data.');
      console.error(e);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 p-4 sm:p-8 font-inter">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center pb-6 border-b border-indigo-100">
          <h1 className="text-3xl font-extrabold text-indigo-700 flex items-center gap-2">
            <ChartBarIcon className="w-8 h-8 text-indigo-500" /> Cosmos Dashboard
          </h1>
          {/* Removed Auth Buttons */}
        </div>

        {/* Global Error Message */}
        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded-md shadow-md mt-4 flex items-center justify-between" role="alert">
            <p className="font-medium">Error: {error}</p>
            <button onClick={() => setError(null)} className="ml-4 text-red-500 hover:text-red-800 transition">
              <XIcon className="w-5 h-5" />
            </button>
          </div>
        )}

        {/* Main Content Layout */}
        <div className="mt-8 grid grid-cols-1 lg:grid-cols-3 gap-6">

          {/* Records Table */}
          <div className="lg:col-span-2 bg-white p-6 rounded-xl shadow-lg border border-gray-200">
            <h2 className="text-xl font-semibold text-gray-800 mb-4">Transaction Records ({records.length})</h2>

            {loading && !error ? (
              <div className="text-center py-10 text-gray-500">Loading data...</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-indigo-50">
                    <tr>
                      <th className="px-4 py-2 text-left text-xs font-medium text-indigo-600 uppercase tracking-wider rounded-tl-lg">ID</th>
                      <th className="px-4 py-2 text-left text-xs font-medium text-indigo-600 uppercase tracking-wider">Category</th>
                      <th className="px-4 py-2 text-left text-xs font-medium text-indigo-600 uppercase tracking-wider">Value A</th>
                      <th className="px-4 py-2 text-left text-xs font-medium text-indigo-600 uppercase tracking-wider rounded-tr-lg">Value B</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-100">
                    {records.length === 0 && !loading && (
                      <tr>
                        <td colSpan="4" className="px-4 py-4 text-sm text-gray-500 text-center">No records found. Click "Add Sample" to populate.</td>
                      </tr>
                    )}
                    {records.map(r =>
                      <tr key={r.id} className="hover:bg-gray-50 transition duration-150">
                        <td className="px-4 py-2 whitespace-nowrap text-sm font-mono text-gray-600">{r.id.substring(0, 8)}...</td>
                        <td className="px-4 py-2 whitespace-nowrap text-sm text-gray-900">{r.category}</td>
                        <td className="px-4 py-2 whitespace-nowrap text-sm text-gray-900 font-medium">{r.valueA}</td>
                        <td className="px-4 py-2 whitespace-nowrap text-sm text-gray-900 font-medium">{r.valueB}</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Chart and Controls */}
          <div className="lg:col-span-1 flex flex-col gap-6">
            <div className="bg-white p-6 rounded-xl shadow-lg border border-gray-200 flex-grow">
              <h2 className="text-xl font-semibold text-gray-800 mb-4">Visual Data</h2>
              <div style={{ height: 300 }}>
                <canvas id="dataChart"></canvas>
              </div>
            </div>

            <div className="bg-white p-6 rounded-xl shadow-lg border border-gray-200">
              <h3 className="text-lg font-semibold text-gray-800 mb-3">Controls</h3>
              <button
                onClick={addSample}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 border border-transparent text-sm font-medium rounded-lg shadow-sm text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition duration-150 ease-in-out"
              >
                <PlusIcon className="w-5 h-5" /> Add New Sample Data
              </button>

              {sampleError && (
                <div className="bg-yellow-50 border border-yellow-300 text-yellow-800 px-3 py-2 rounded-md mt-4 text-sm" role="alert">
                  <p className="font-medium">Warning: {sampleError}</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default App;
