import logoPng from '@/assets/brand/logo.png'

interface BrandLogoProps {
  className?: string
}

export function BrandLogo({ className = 'h-8 w-auto' }: BrandLogoProps) {
  return <img src={logoPng} alt="Doclyn" className={className} />
}
