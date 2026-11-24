import type { Metadata } from "next";
import "./globals.css";
export const metadata: Metadata = { title: "Private Enterprise Intelligence", description: "Secure document intelligence powered by self-hosted AI." };
export default function RootLayout({children}:{children:React.ReactNode}) {
  return <html lang="en"><body>{children}</body></html>;
}
