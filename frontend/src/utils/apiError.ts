import { AxiosError } from "axios";

type ApiErrorPayload = {
  message?: string;
  validationErrors?: Record<string, string>;
};

export const extractApiError = (
  error: unknown,
  fallbackMessage: string,
): { message: string; validationErrors: Record<string, string> } => {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiErrorPayload | undefined;
    return {
      message: data?.message || fallbackMessage,
      validationErrors: data?.validationErrors ?? {},
    };
  }

  return {
    message: fallbackMessage,
    validationErrors: {},
  };
};
