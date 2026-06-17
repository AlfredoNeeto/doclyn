import * as React from 'react'
import { cn } from '@/lib/utils'
function DropdownMenu({ trigger, children }: { trigger: React.ReactNode; children: React.ReactNode }) {
  const [open, setOpen] = React.useState(false)
  const ref = React.useRef<HTMLDivElement>(null)
  React.useEffect(() => {
    const handler = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])
  return (
    <div className="relative inline-block" ref={ref}>
      <div onClick={() => setOpen(!open)}>{trigger}</div>
      {open && <div className="absolute right-0 z-50 mt-2 w-56 rounded-md border bg-popover p-1 text-popover-foreground shadow-md" onClick={() => setOpen(false)}>{children}</div>}
    </div>
  )
}
function DropdownMenuItem({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('flex cursor-pointer items-center rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent', className)} {...props} />
}
function DropdownMenuSeparator() {
  return <div className="-mx-1 my-1 h-px bg-border" />
}
export { DropdownMenu, DropdownMenuItem, DropdownMenuSeparator }
