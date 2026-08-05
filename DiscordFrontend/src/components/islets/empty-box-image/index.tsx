import Image from "next/image";

interface EmptyBoxProps {
  src: string;
  text: string;
  alt: string;
}

export const EmptyBox = ({ src, text, alt }: EmptyBoxProps) => {
  return (
    <div className="flex h-full flex-1 flex-col items-center justify-center gap-8">
      <div className="relative h-[300px] w-[300px]">
        <Image
          fill
          className="object-contain grayscale"
          src={src}
          alt={alt}
          sizes="300px"
          loading="eager"
        />
      </div>

      <p className="text-gray-400">{text}</p>
    </div>
  );
};