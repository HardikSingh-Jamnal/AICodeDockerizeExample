import React, { useState } from "react";
import { Search, Package, ShoppingCart, Truck, User, Filter } from "lucide-react";

interface Offer {
  id: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  price: string;
  owner: string;
}

interface Purchase {
  id: string;
  offerId: string;
  buyer: string;
  status: string;
}

interface Transport {
  id: string;
  vehicle: string;
  carrier: string;
  status: string;
}

const mockResults = {
  offers: [
    { id: "O-123", vin: "1HGCM82633A004352", make: "Toyota", model: "Camry", year: 2022, price: "$22,000", owner: "Seller A" },
    { id: "O-124", vin: "2HGCM82633A004353", make: "Honda", model: "Accord", year: 2023, price: "$25,500", owner: "Seller B" },
  ],
  purchases: [
    { id: "P-456", offerId: "O-123", buyer: "Buyer X", status: "Completed" },
    { id: "P-457", offerId: "O-124", buyer: "Buyer Y", status: "Pending" },
  ],
  transports: [
    { id: "T-789", vehicle: "Camry 2022", carrier: "Carrier Y", status: "In Transit" },
    { id: "T-790", vehicle: "Accord 2023", carrier: "Carrier Z", status: "Delivered" },
  ],
};

export default function CentralizedSearchMock() {
  const [query, setQuery] = useState("");
  const [role, setRole] = useState("Agent");
  const [activeTab, setActiveTab] = useState("all");

  const filteredResults = {
    offers: mockResults.offers.filter((o: Offer) =>
      query === "" ||
      o.make.toLowerCase().includes(query.toLowerCase()) ||
      o.model.toLowerCase().includes(query.toLowerCase()) ||
      o.vin.toLowerCase().includes(query.toLowerCase())
    ),
    purchases: mockResults.purchases.filter((p: Purchase) =>
      query === "" ||
      p.id.toLowerCase().includes(query.toLowerCase()) ||
      p.buyer.toLowerCase().includes(query.toLowerCase())
    ),
    transports: mockResults.transports.filter((t: Transport) =>
      query === "" ||
      t.id.toLowerCase().includes(query.toLowerCase()) ||
      t.vehicle.toLowerCase().includes(query.toLowerCase())
    ),
  };

  const tabs = [
    { id: "all", label: "All Results", icon: Search },
    { id: "offers", label: "Offers", icon: Package },
    { id: "purchases", label: "Purchases", icon: ShoppingCart },
    { id: "transports", label: "Transports", icon: Truck },
  ];

  const showSection = (section: string) => activeTab === "all" || activeTab === section;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100">
      {/* Header */}
      <div className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h1 className="text-3xl font-bold text-slate-900">Search Hub</h1>
              <p className="text-slate-600 mt-1">Find offers, purchases, and transports</p>
            </div>

            <div className="flex items-center gap-3 bg-slate-100 px-4 py-2 rounded-full">
              <User className="w-5 h-5 text-slate-600" />
              <select
                className="bg-transparent border-none outline-none font-medium text-slate-900 cursor-pointer"
                value={role}
                onChange={(e) => setRole(e.target.value)}
              >
                <option>Seller</option>
                <option>Buyer</option>
                <option>Carrier</option>
                <option>Agent</option>
              </select>
            </div>
          </div>

          {/* Search Bar */}
          <div className="relative">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400" />
            <input
              className="w-full pl-12 pr-4 py-4 rounded-xl bg-slate-50 border-2 border-slate-200 focus:border-blue-500 focus:bg-white transition-all outline-none text-slate-900 placeholder-slate-400"
              placeholder="Search by VIN, vehicle, buyer, or ID..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white border-b border-slate-200">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex gap-1 overflow-x-auto">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 px-6 py-4 font-medium transition-all border-b-2 whitespace-nowrap ${isActive
                    ? "text-blue-600 border-blue-600"
                    : "text-slate-600 border-transparent hover:text-slate-900 hover:bg-slate-50"
                    }`}
                >
                  <Icon className="w-4 h-4" />
                  {tab.label}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-6 py-8">
        <div className="space-y-8">
          {showSection("offers") && (
            <Section
              title="Offers"
              icon={Package}
              items={filteredResults.offers}
              render={(o: Offer) => (
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <span className="text-lg font-bold text-slate-900">
                        {o.year} {o.make} {o.model}
                      </span>
                      <span className="px-2 py-1 bg-emerald-100 text-emerald-700 text-xs font-semibold rounded-full">
                        {o.price}
                      </span>
                    </div>
                    <div className="text-sm text-slate-600 space-y-1">
                      <div>VIN: <span className="font-mono text-slate-900">{o.vin}</span></div>
                      <div>Owner: <span className="font-medium text-slate-900">{o.owner}</span></div>
                    </div>
                  </div>
                  <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium text-sm">
                    View Details
                  </button>
                </div>
              )}
            />
          )}

          {showSection("purchases") && (
            <Section
              title="Purchases"
              icon={ShoppingCart}
              items={filteredResults.purchases}
              render={(p: Purchase) => (
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <span className="text-lg font-bold text-slate-900">Purchase {p.id}</span>
                      <span className={`px-2 py-1 text-xs font-semibold rounded-full ${p.status === "Completed"
                        ? "bg-emerald-100 text-emerald-700"
                        : "bg-amber-100 text-amber-700"
                        }`}>
                        {p.status}
                      </span>
                    </div>
                    <div className="text-sm text-slate-600 space-y-1">
                      <div>Offer ID: <span className="font-mono text-slate-900">{p.offerId}</span></div>
                      <div>Buyer: <span className="font-medium text-slate-900">{p.buyer}</span></div>
                    </div>
                  </div>
                  <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium text-sm">
                    View Details
                  </button>
                </div>
              )}
            />
          )}

          {showSection("transports") && (
            <Section
              title="Transports"
              icon={Truck}
              items={filteredResults.transports}
              render={(t: Transport) => (
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <span className="text-lg font-bold text-slate-900">Transport {t.id}</span>
                      <span className={`px-2 py-1 text-xs font-semibold rounded-full ${t.status === "Delivered"
                        ? "bg-emerald-100 text-emerald-700"
                        : "bg-blue-100 text-blue-700"
                        }`}>
                        {t.status}
                      </span>
                    </div>
                    <div className="text-sm text-slate-600 space-y-1">
                      <div>Vehicle: <span className="font-medium text-slate-900">{t.vehicle}</span></div>
                      <div>Carrier: <span className="font-medium text-slate-900">{t.carrier}</span></div>
                    </div>
                  </div>
                  <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium text-sm">
                    Track
                  </button>
                </div>
              )}
            />
          )}

          {filteredResults.offers.length === 0 &&
            filteredResults.purchases.length === 0 &&
            filteredResults.transports.length === 0 && (
              <div className="text-center py-16">
                <div className="inline-flex items-center justify-center w-16 h-16 bg-slate-100 rounded-full mb-4">
                  <Search className="w-8 h-8 text-slate-400" />
                </div>
                <h3 className="text-xl font-semibold text-slate-900 mb-2">No results found</h3>
                <p className="text-slate-600">Try adjusting your search terms</p>
              </div>
            )}
        </div>
      </div>
    </div>
  );
}

function Section<T>({ title, icon: Icon, items, render }: {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  items: T[];
  render: (item: T) => React.ReactNode;
}) {
  if (items.length === 0) return null;

  return (
    <div>
      <div className="flex items-center gap-2 mb-4">
        <Icon className="w-5 h-5 text-slate-600" />
        <h2 className="text-xl font-bold text-slate-900">{title}</h2>
        <span className="px-2 py-0.5 bg-slate-200 text-slate-700 text-xs font-semibold rounded-full">
          {items.length}
        </span>
      </div>
      <div className="grid gap-4">
        {items.map((item, idx) => (
          <div
            key={idx}
            className="p-6 rounded-xl bg-white border border-slate-200 hover:border-slate-300 hover:shadow-md transition-all"
          >
            {render(item)}
          </div>
        ))}
      </div>
    </div>
  );
}