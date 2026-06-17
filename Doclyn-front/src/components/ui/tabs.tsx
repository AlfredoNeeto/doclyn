import * as React from 'react'
import { cn } from '@/lib/utils'
const TabsContext = React.createContext<{ activeTab: string; setActiveTab: (t: string) => void } | null>(null)
function Tabs({ defaultValue, children, className }: { defaultValue: string; children: React.ReactNode; className?: string }) {
  const [activeTab, setActiveTab] = React.useState(defaultValue)
  return <TabsContext.Provider value={{ activeTab, setActiveTab }}><div className={className}>{children}</div></TabsContext.Provider>
}
function TabsList({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn('inline-flex h-10 items-center justify-center rounded-md bg-muted p-1 text-muted-foreground', className)}>{children}</div>
}
function TabsTrigger({ value, children, className }: { value: string; children: React.ReactNode; className?: string }) {
  const ctx = React.useContext(TabsContext)
  if (!ctx) throw new Error('TabsTrigger within Tabs')
  return <button className={cn('inline-flex items-center justify-center whitespace-nowrap rounded-sm px-3 py-1.5 text-sm font-medium transition-all', ctx.activeTab === value && 'bg-background text-foreground shadow-sm', className)} onClick={() => ctx.setActiveTab(value)}>{children}</button>
}
function TabsContent({ value, children, className }: { value: string; children: React.ReactNode; className?: string }) {
  const ctx = React.useContext(TabsContext)
  if (!ctx) throw new Error('TabsContent within Tabs')
  if (ctx.activeTab !== value) return null
  return <div className={cn('mt-2', className)}>{children}</div>
}
export { Tabs, TabsList, TabsTrigger, TabsContent }
