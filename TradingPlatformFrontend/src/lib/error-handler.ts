import { toast } from "sonner"
import axios from "axios"

export const handleApiError = (error: unknown) => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as any
    const status = error.response?.status

    // Extract message from our standard ApiResponse structure
    if (data?.message || (data?.errors && data.errors.length > 0)) {
      const message = data.message || "Action failed"
      const details = Array.isArray(data.errors) 
        ? data.errors.join(". ") 
        : typeof data.errors === 'string' ? data.errors : undefined
        
      toast.error(message, {
        description: details,
      })
      return
    }

    // Handle standard Problem Details (RFC 7807) used by .NET
    if (data?.title || data?.detail) {
      toast.error(data.title || "Error", {
        description: data.detail || (data.errors ? JSON.stringify(data.errors) : undefined)
      })
      return
    }

    switch (status) {
      case 400:
        toast.error("Invalid Request", { description: "The server could not understand the request." })
        break
      case 401:
        // Silently handled by auth interceptor usually
        break
      case 402:
        toast.error("Insufficient Funds", { description: "You don't have enough balance for this operation." })
        break
      case 403:
        toast.error("Access Denied", { description: "You don't have permission to perform this action." })
        break
      case 404:
        toast.error("Not Found", { description: "The requested resource was not found." })
        break
      case 415:
        toast.error("Unsupported Format", { description: "The request data format is not supported." })
        break
      case 429:
        toast.error("Too Many Requests", { description: "Please slow down and try again later." })
        break
      case 500:
      case 502:
      case 503:
      case 504:
        toast.error("Server Error", { description: "Something went wrong on our side. We're looking into it." })
        break
      default:
        toast.error("Network Error", { description: "Please check your connection or try again later." })
    }
  } else {
    toast.error("Unexpected Error", {
      description: error instanceof Error ? error.message : "Something went wrong"
    })
  }
}
