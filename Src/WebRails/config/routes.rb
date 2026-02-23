Rails.application.routes.draw do
  root "dashboard#index"

  resources :projects, only: [:index, :show, :destroy] do
    member do
      post :rescan
      post :toggle_pin
      post :open_terminal
      post :open_editor
    end
  end

  get "activity", to: "activity#index"

  resource :settings, only: [:show, :update] do
    post :add_folder
    delete :remove_folder
  end

  resources :scans, only: [:create]
end
