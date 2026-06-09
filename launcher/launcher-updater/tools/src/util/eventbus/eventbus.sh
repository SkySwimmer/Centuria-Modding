#!/bin/bash

# Require
require "util.arrayutil"

# Declare event list
declare -g EventBus_events=()

# Fire event
function EventBus.fireEvent() {
    local args=("$@")
    local event="$1"
    event="${event//./_}"   
    
    local parms=()
    arrayCopyOfRange args params 1 "${#args[@]}"
    if ! arrayContains "$event" EventBus_events ; then
        # Event not bound
        return
    fi

    # Get events
    eval 'local eventlisteners=("${EventBus_eventlisteners_'"$event"'[@]}")'
    for listener in "${eventlisteners[@]}"; do
        runFunctionSafe "$listener" "${params[@]}"
    done
}

# Binding of listeners
function EventBus.bindEvent() {
    local handler="$1"
    local event="$2"
    event="${event//./_}"

    # Create event if needed
    if ! arrayContains "$event" EventBus_events ; then
        # Register
        EventBus_events+=("$event")
        eval 'EventBus_eventlisteners_'"$event"'=()'
    fi
    
    # Check
    if ! arrayContains "$handler" "EventBus_eventlisteners_$event"; then
        # Bind
        arrayAdd "$handler" "EventBus_eventlisteners_$event"
        eval "EventBus_eventmap_$(echo "$handler" | sed "s/\\./_/g" | sed "s/\\:/_/g" | sed "s/\\@/_/g")=\"\$event\""
    fi
}

# Unbinding of listeners
function EventBus.unbindEvent() {
    local handler="$1"
    local event="$2"
    if [ "$event" == "" ]; then 
        eval 'event="$EventBus_eventmap_'"$(echo "$handler" | sed "s/\\./_/g" | sed "s/\\:/_/g" | sed "s/\\@/_/g")"'"'
    fi
    if [ "$event" == "" ]; then 
        return
    fi

    # Check event
    if ! arrayContains "$event" EventBus_events ; then
        # Unregistered
        return
    fi
    
    # Check
    if arrayContains "$handler" "EventBus_eventlisteners_$event"; then
        # Upate
        arrayRemove "$handler" "EventBus_eventlisteners_$event"
        eval "EventBus_eventmap_$(echo "$handler" | sed "s/\\./_/g" | sed "s/\\:/_/g" | sed "s/\\@/_/g")=\"\""
    fi
}


# Bind init finish
function EventBus.internalFinishSourceInit() {
    # Find all functions
    while read -r def; do
        # Parse
        def="${def//declare -f /}"
        
        # Check
        if [[ "$def" == *"@event:"* ]]; then
            # Bind event
            local listener="$def"
            local event="$(echo "$def" | sed "s/.*\@event://g")"
            local proxyname="$(echo "$listener" | sed "s/\@event:.*//g")"

            # Create proxy
            eval 'function '"$proxyname"'() { '"$listener"' "$@" ; }'
            
            # Bind
            EventBus.bindEvent "$proxyname" "$event"
        fi
    done < <(declare -fF)
}
EventBus.bindEvent EventBus.internalFinishSourceInit Core.finishSourceInit
