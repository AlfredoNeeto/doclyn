export interface PagedResponse<T> {
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  items: T[]
}
