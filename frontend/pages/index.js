import Link from 'next/link';

export default function Home() {
  return (
    <div>
      <h1>Welcome to the Microservices App</h1>
      <nav>
        <ul>
          <li><Link href="/order"><a>Place an Order</a></Link></li>
          <li><Link href="/billing"><a>View Billing Details</a></Link></li>
        </ul>
      </nav>
      <h2>Products</h2>
      {/* Product listing will go here */}
    </div>
  );
}
