import Link from 'next/link'

export default function Home() {
  return (
    <div>
      <h1>Welcome to the Microservices App</h1>
      <nav>
        <ul>
          <li>
            <Link href="/order">Place an Order</Link>
          </li>
          <li>
            <Link href="/billing">View Billing Details</Link>
          </li>
        </ul>
      </nav>
      <h2>Products</h2>
      <p>Product listing will go here</p>
    </div>
  )
}