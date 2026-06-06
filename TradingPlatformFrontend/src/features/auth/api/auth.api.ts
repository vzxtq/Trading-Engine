import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/api/axios'
import type { ApiResponse, LoginResponseDto, AccountDto, PositionDto } from '@/types'
import type { LoginRequest, RegisterRequest, UpdateProfileRequest, UpdatePasswordRequest } from '../types/auth-requests.types'
import { useAuthStore } from '@/store/auth'

export const useLogin = () => {
  const setAuth = useAuthStore(state => state.setAuth)

  return useMutation({
    mutationFn: async (data: LoginRequest) => {
      const response = await api.post<ApiResponse<LoginResponseDto>>('/accounts/login', data)
      return response.data
    },
    onSuccess: (response) => {
      if (response.success && response.data) {
        const { token, refreshToken, userId, email } = response.data
        setAuth(token, refreshToken, userId, email)
      }
    },
  })
}

export const useRegister = () => {
  const setAuth = useAuthStore(state => state.setAuth)

  return useMutation({
    mutationFn: async (data: RegisterRequest) => {
      const response = await api.post<ApiResponse<LoginResponseDto>>('/accounts/register', data)
      return response.data
    },
    onSuccess: (response) => {
      if (response.success && response.data) {
        const { token, refreshToken, userId, email } = response.data
        setAuth(token, refreshToken, userId, email)
      }
    },
  })
}

export const useUpdateProfile = () => {
  return useMutation({
    mutationFn: async (data: UpdateProfileRequest) => {
      const response = await api.put<ApiResponse<null>>('/accounts', data)
      return response.data
    },
  })
}

export const useUpdatePassword = () => {
  return useMutation({
    mutationFn: async (data: Omit<UpdatePasswordRequest, 'confirmPassword'>) => {
      const response = await api.put<ApiResponse<null>>('/accounts', data)
      return response.data
    },
  })
}

export const useAccount = (userId: string | null) => {
  return useQuery({
    queryKey: ['account', userId],
    queryFn: async () => {
      if (!userId) return null
      const response = await api.get<ApiResponse<AccountDto>>(`/accounts/${userId}`)
      return response.data.data
    },
    enabled: !!userId,
  })
}

export const usePositions = () => {
  return useQuery({
    queryKey: ['positions'],
    queryFn: async () => {
      const response = await api.get<ApiResponse<PositionDto[]>>('/accounts/positions')
      return response.data.data || []
    },
  })
}
