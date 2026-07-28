import type { Metadata } from "next";
import { Open_Sans } from "next/font/google";
import CommonLayout from "@/components/layout/common-layout";
import CurrentUserLoader from "@/components/current-user-loader";
import "./globals.css";

const mainFont = Open_Sans({
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Discord Clone",
  description: "Discord Clone application",
};

export default function RootLayout({
  children,
}: React.PropsWithChildren) {
  return (
    <html lang="en">
      <body className={mainFont.className + " dark"}>
        <CurrentUserLoader />
        <CommonLayout />
        {children}
      </body>
    </html>
  );
}